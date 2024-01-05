/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionsController
 * @created     : Friday Jan 05, 2024 13:00:42 CST
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
public partial class ChatController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly LlamaModelManager _modelManager;
	private readonly LlamaInferenceService _inferenceService;

	public ChatController(IMapper mapper)
	{
		_mapper = mapper;
		 _modelManager = ServiceRegistry.Get<LlamaModelManager>();
		 _inferenceService = ServiceRegistry.Get<LlamaInferenceService>();
	}

    [HttpPost("completions", Name = nameof(CreateChatCompletion))]
    public async Task<ActionResult<ChatCompletionDto>> CreateChatCompletion(ApiVersion version, [FromBody] ChatCompletionCreateDto chatCompletionCreateDto)
    {
    	LoggerManager.LogDebug("Recieved chatCompletionCreateDto", "", "create", chatCompletionCreateDto);

    	return Ok();
    }
}

