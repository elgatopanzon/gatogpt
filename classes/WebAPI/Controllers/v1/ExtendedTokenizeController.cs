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
	private readonly TextGenerationService _textGenerationService;

	public ExtendedTokenizeController(IMapper mapper)
	{
		_mapper = mapper;
		 _textGenerationService = ServiceRegistry.Get<TextGenerationService>();
	}

    [HttpPost(Name = nameof(Tokenize))]
    public ActionResult<ExtendedTokenizeDto> Tokenize(ApiVersion version, [FromBody] ExtendedTokenizeCreateDto tokenizeCreateDto)
    {
    	LoggerManager.LogDebug("Tokenize tokenizeCreateDto", "", "tokenizeCreateDto", tokenizeCreateDto);

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

    	LoggerManager.LogDebug("Returning tokenizedStringDto", "", "tokenizedStringDto", tokenizedStringDto);

		return Ok(tokenizedStringDto);
    }

    [HttpPost("chat", Name = nameof(TokenizeChatCompletion))]
    public ActionResult<ExtendedTokenizeDto> TokenizeChatCompletion(ApiVersion version, [FromBody] ChatCompletionCreateDto chatCompletionCreateDto)
    {
    	LoggerManager.LogDebug("Tokenize chatCompletionCreateDto", "", "chatCompletionCreateDto", chatCompletionCreateDto);
		return Ok();
    }
}
