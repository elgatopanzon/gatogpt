/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ExtendedTokenizeController
 * @created     : Wednesday Jan 31, 2024 17:29:07 CST
 */

namespace GatoGPT.WebAPI.v1.Controllers;

using GatoGPT.Service;
using GatoGPT.Config;
using GatoGPT.WebAPI.Dtos;
using GatoGPT.WebAPI.Entities;
using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.AI.OpenAI;

using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

[ApiController]
[ApiVersion("1.0")]
[Tags("Tokenize (Extended API)")]
[Route("v{version:apiVersion}/extended/tokenize")]
public partial class ExtendedTokenizeController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly TextGenerationModelManager _modelManager;
	private readonly TextGenerationService _textGenerationService;

	public ExtendedTokenizeController(IMapper mapper)
	{
		_mapper = mapper;
		 _modelManager = ServiceRegistry.Get<TextGenerationModelManager>();
		 _textGenerationService = ServiceRegistry.Get<TextGenerationService>();
	}

    [HttpPost(Name = nameof(Tokenize))]
    public async Task<ActionResult<ExtendedTokenizeDto>> Tokenize(ApiVersion version, [FromBody] ExtendedTokenizeCreateDto tokenizeCreateDto)
    {
    	LoggerManager.LogDebug("Tokenize tokenizeCreateDto", "", "tokenizeCreateDto", tokenizeCreateDto);

    	// validate required params
		if (tokenizeCreateDto.Model.Length == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a model parameter",
						code: null,
						param:null
						));
		}

		// check model is valid
		if (!_modelManager.ModelDefinitions.ContainsKey(tokenizeCreateDto.Model))
		{
    		return NotFound(new InvalidRequestErrorDto(message:$"The model '{tokenizeCreateDto.Model}' does not exist", code:"model_not_found", param:"model"));
		}

    	var tokenizedStringDto = new ExtendedTokenizeDto();

    	try
    	{
    		var tokenized = _textGenerationService.TokenizeString(tokenizeCreateDto.Model, tokenizeCreateDto.Content);

    		tokenizedStringDto.Tokens = tokenized;
    	}
    	catch (System.Exception e)
    	{
			ErrorResultExtended err = new() {
				Error = new() {
					Code = "tokenize_error",
					Type = e.GetType().Name,
					Message = e.Message,
					Exception = e,
				}
			};

			LoggerManager.LogDebug("Tokenize content exception", "", "error", err);

			return BadRequest(err);
    	}

    	LoggerManager.LogDebug("Tokenized string count", "", tokenizeCreateDto.Content, tokenizedStringDto.Tokens.Count);

    	LoggerManager.LogDebug("Returning tokenizedStringDto", "", "tokenizedStringDto", tokenizedStringDto);

		return Ok(tokenizedStringDto);
    }

    [HttpPost("chat", Name = nameof(TokenizeChatCompletion))]
    public async Task<ActionResult<ExtendedTokenizeDto>> TokenizeChatCompletion(ApiVersion version, [FromBody] ChatCompletionCreateDto chatCompletionCreateDto)
    {
    	LoggerManager.LogDebug("Tokenize chatCompletionCreateDto", "", "chatCompletionCreateDto", chatCompletionCreateDto);

    	// validate required params
		if (chatCompletionCreateDto.Model.Length == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a model parameter",
						code: null,
						param:null
						));
		}

		// check model is valid
		if (!_modelManager.ModelDefinitions.ContainsKey(chatCompletionCreateDto.Model))
		{
    		return NotFound(new InvalidRequestErrorDto(message:$"The model '{chatCompletionCreateDto.Model}' does not exist", code:"model_not_found", param:"model"));
		}

    	ModelDefinition modelDefinition = _modelManager.GetModelDefinition(chatCompletionCreateDto.Model);

    	// create instance of model and get full prompt
    	var modelInstance = _textGenerationService.CreateModelInstance(modelDefinition.Id);

    	var inferenceParams = modelDefinition.ModelProfile.InferenceParams.DeepCopy();
    	var loadParams = modelDefinition.ModelProfile.LoadParams.DeepCopy();
    	modelInstance.InferenceParams = inferenceParams;
    	modelInstance.LoadParams = loadParams;

		// apply extended parameters which can affect token length
		if (chatCompletionCreateDto.Extended != null)
		{
			if (chatCompletionCreateDto.Extended.Model != null)
			{
				if (chatCompletionCreateDto.Extended.Model.Backend != null)
					modelDefinition.Backend = (string) chatCompletionCreateDto.Extended.Model.Backend;
			}
			if (chatCompletionCreateDto.Extended.Inference != null)
			{
				if (chatCompletionCreateDto.Extended.Inference.ChatMessageTemplate != null)
					inferenceParams.ChatMessageTemplate = (string) chatCompletionCreateDto.Extended.Inference.ChatMessageTemplate;
				if (chatCompletionCreateDto.Extended.Inference.ChatMessageGenerationTemplate != null)
					inferenceParams.ChatMessageGenerationTemplate = (string) chatCompletionCreateDto.Extended.Inference.ChatMessageGenerationTemplate;
				if (chatCompletionCreateDto.Extended.Inference.PrePrompt != null)
					inferenceParams.PrePrompt = (string) chatCompletionCreateDto.Extended.Inference.PrePrompt;
				if (chatCompletionCreateDto.Extended.Inference.CfgNegativePrompt != null)
					inferenceParams.NegativeCfgPrompt = (string) chatCompletionCreateDto.Extended.Inference.CfgNegativePrompt;
			}
		}

    	// create a StatefulChat instance and parse the prompt
    	StatefulChat chatInstance = new(false, loadParams, inferenceParams);

		List<StatefulChatMessage> chatMessages = new();
    	foreach (var message in chatCompletionCreateDto.Messages)
    	{
    		chatMessages.Add(new() {
				Role = message.Role,
				Name = message.Name,
				Content = message.GetContent(),
    		});
    	}

    	chatInstance.SetChatMessages(chatMessages);
		chatInstance.UpdateStatefulInstanceId();

    	string chatPrompt = chatInstance.GetPrompt();

    	chatPrompt = modelInstance.FormatPrompt(chatPrompt);

		// create tokenizeDto
    	ExtendedTokenizeCreateDto tokenizeCreateDto = new() {
			Model = chatCompletionCreateDto.Model,
			Content = chatPrompt+inferenceParams.NegativeCfgPrompt,
    	};

		var res = await Tokenize(HttpContext.GetRequestedApiVersion(), tokenizeCreateDto);

		if (res.Result is OkObjectResult okRes)
		{
			LoggerManager.LogDebug("Tokenize result", "", "res", okRes.Value);

			return Ok(okRes.Value);
		}
		else if (res.Result is BadRequestResult badRes)
		{
    		return BadRequest(badRes);
		}
		else {
    		return BadRequest(new InvalidRequestErrorDto(message:$"Unknown error", code:"tokenize_unknown_error"));
		}

    }
}
