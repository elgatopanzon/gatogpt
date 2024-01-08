/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionsController
 * @created     : Friday Jan 05, 2024 00:03:44 CST
 */

namespace GatoGPT.WebAPI.v1.Controllers;

using GatoGPT.Service;
using GatoGPT.Config;
using GatoGPT.LLM;
using GatoGPT.WebAPI.Dtos;
using GatoGPT.WebAPI.Entities;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using AutoMapper;
// using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
// using GatoGPT.WebAPI.Dtos;
// using GatoGPT.WebAPI.Entities;
using System.Text.Json;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public partial class CompletionsController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly LlamaModelManager _modelManager;
	private readonly LlamaInferenceService _inferenceService;

	public CompletionsController(IMapper mapper)
	{
		_mapper = mapper;
		 _modelManager = ServiceRegistry.Get<LlamaModelManager>();
		 _inferenceService = ServiceRegistry.Get<LlamaInferenceService>();
	}

    [HttpPost(Name = nameof(CreateCompletion))]
    public async Task<ActionResult<CompletionDto>> CreateCompletion(ApiVersion version, [FromBody] CompletionCreateDto completionCreateDto)
    {
    	LoggerManager.LogDebug("Recieved completionCreateDto", "", "create", completionCreateDto);

    	// validate required params
		if (completionCreateDto.Model.Length == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a model parameter",
						code: null,
						param:null
						));
		}

		// check model is valid
		if (!_modelManager.ModelDefinitions.ContainsKey(completionCreateDto.Model))
		{
    		return NotFound(new InvalidRequestErrorDto(message:$"The model '{completionCreateDto.Model}' does not exist", code:"model_not_found", param:"model"));
		}

		// extract prompts
		List<string> prompts = new();
		if (completionCreateDto.Prompt is System.String promptString)
		{
			prompts.Add(promptString);
		}
		else if (completionCreateDto.Prompt is Newtonsoft.Json.Linq.JArray promptList)
		{
			prompts = promptList.ToArray().Select(x => x.ToString()).ToList();
		}

		// extract stops
		List<string> stops = new();
		if (completionCreateDto.Stop is System.String stopString)
		{
			stops.Add(stopString);
		}
		else if (completionCreateDto.Stop is Newtonsoft.Json.Linq.JArray stopsList)
		{
			stops = stopsList.ToArray().Select(x => x.ToString()).ToList();
		}

		LoggerManager.LogDebug("Completion dto extracted prompts", "", "prompts", prompts);
		LoggerManager.LogDebug("Completion dto extracted stops", "", "stops", stops);

		// queue and generate responses
		List<LlamaModelInstance> inferenceInstances = new();

		// create LoadParams and InferenceParams objects from dto
		LLM.LoadParams loadParams = _modelManager.GetModelDefinition(completionCreateDto.Model).ModelProfile.LoadParams.DeepCopy();
		LLM.InferenceParams inferenceParams = _modelManager.GetModelDefinition(completionCreateDto.Model).ModelProfile.InferenceParams.DeepCopy();

		var completionCreateDtoDefault = new CompletionCreateDto();

		if (completionCreateDto.Temperature != completionCreateDtoDefault.Temperature)
			inferenceParams.Temp = completionCreateDto.Temperature;
		if (completionCreateDto.FrequencyPenalty != completionCreateDtoDefault.FrequencyPenalty)
			inferenceParams.FrequencyPenalty = completionCreateDto.FrequencyPenalty;
		if (completionCreateDto.PresencePenalty != completionCreateDtoDefault.PresencePenalty)
			inferenceParams.PresencePenalty = completionCreateDto.PresencePenalty;
		if (completionCreateDto.RepeatPenalty != completionCreateDtoDefault.RepeatPenalty)
			inferenceParams.RepeatPenalty = completionCreateDto.RepeatPenalty;
		if (completionCreateDto.MaxTokens != completionCreateDtoDefault.MaxTokens)
			inferenceParams.NPredict = completionCreateDto.MaxTokens;
		if (completionCreateDto.Seed != completionCreateDtoDefault.Seed)
			loadParams.Seed = completionCreateDto.Seed;

		inferenceParams.Antiprompts = inferenceParams.Antiprompts.Concat(stops).ToList();

		if (completionCreateDto.MinP != completionCreateDtoDefault.MinP)
			inferenceParams.MinP = completionCreateDto.MinP;
		if (completionCreateDto.TopP != completionCreateDtoDefault.TopP)
			inferenceParams.TopP = completionCreateDto.TopP;
		if (completionCreateDto.TopK != completionCreateDtoDefault.TopK)
			inferenceParams.TopK = completionCreateDto.TopK;

		// create N * prompts count instances
		foreach (string prompt in prompts)
		{
			for (int i = 0; i < completionCreateDto.N; i++)
			{
				var modelInstance = _inferenceService.Infer(completionCreateDto.Model, prompt, stateful:false, loadParams:loadParams, inferenceParams:inferenceParams);	

				inferenceInstances.Add(modelInstance);
			}			
		}

		// wait for them all to be finished
		while (true)
		{
			int finishedCount = 0;

			foreach (var instance in inferenceInstances)
			{
				if (instance.Finished)
				{
					finishedCount++;
				}
			}

			if (finishedCount == inferenceInstances.Count)
			{
				break;
			}

			await Task.Delay(100);
		}

		// create CompletionDto object and populate choice results
		CompletionDto completionDto = new();

		int currentIndex = 0;
		foreach (var instance in inferenceInstances)
		{
			completionDto.Usage.PromptTokens += instance.InferenceResult.PromptTokenCount;
			completionDto.Usage.CompletionTokens += instance.InferenceResult.GenerationTokenCount;

			completionDto.Choices.Add(new CompletionChoiceDto() {
				FinishReason = (instance.InferenceResult.Tokens.Count >= completionCreateDto.MaxTokens ? "length" : "stop"),
				Index = currentIndex,
				Text = instance.InferenceResult.Output,
				InferenceResult = instance.InferenceResult,
				});

			currentIndex++;
		}

		completionDto.Id = $"cmpl-{GetHashCode()}-{completionDto.GetHashCode()}-{completionCreateDto.GetHashCode()}";
		completionDto.Created = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();
		completionDto.Model = completionCreateDto.Model;
		completionDto.SystemFingerprint = GetHashCode().ToString();

		LoggerManager.LogDebug("Returning completionDto", "", "completionDto", completionDto);

    	return Ok(completionDto);
    }
}

