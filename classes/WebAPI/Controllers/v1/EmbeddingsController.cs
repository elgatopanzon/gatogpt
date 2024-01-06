/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingsController
 * @created     : Friday Jan 05, 2024 18:47:39 CST
 */

namespace GatoGPT.WebAPI.v1.Controllers;

using GatoGPT.LLM;

using GatoGPT.Service;
using GatoGPT.Config;
using GatoGPT.Resource;
using GatoGPT.WebAPI.Dtos;
using GatoGPT.WebAPI.Entities;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using LLama;
using LLama.Common;

using AutoMapper;
// using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
// using GatoGPT.WebAPI.Dtos;
// using GatoGPT.WebAPI.Entities;
using System.Text.Json;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public partial class EmbeddingsController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly ResourceManager _resourceManager;
	private readonly EmbeddingModelManager _modelManager;
	private readonly EmbeddingService _embeddingService;

	public EmbeddingsController(IMapper mapper)
	{
		_mapper = mapper;
		 _resourceManager = ServiceRegistry.Get<ResourceManager>();
		 _modelManager = ServiceRegistry.Get<EmbeddingModelManager>();
		 _embeddingService = ServiceRegistry.Get<EmbeddingService>();
	}

    [HttpPost(Name = nameof(CreateEmbedding))]
    public async Task<ActionResult<EmbeddingDto>> CreateEmbedding(ApiVersion version, [FromBody] EmbeddingCreateDto embeddingCreateDto)
    {
    	LoggerManager.LogDebug("Recieved embeddingCreateDto", "", "create", embeddingCreateDto);

    	// // validate required params
		if (embeddingCreateDto.Model.Length == 0)
		{
			return BadRequest(new InvalidRequestErrorDto(
						message: "You must provide a model parameter",
						code: null,
						param:null
						));
		}

		// check model is valid
		if (!_modelManager.ModelDefinitions.ContainsKey(embeddingCreateDto.Model))
		{
    		return NotFound(new InvalidRequestErrorDto(message:$"The model '{embeddingCreateDto.Model}' does not exist", code:"model_not_found", param:"model"));
		}

		// create the EmbeddingsDto object
        EmbeddingsDto embeddingsDto = new EmbeddingsDto() {
			Model = embeddingCreateDto.Model
        };

		// create embeddings for each passed input
        for (int i = 0; i < embeddingCreateDto.GetInputs().Count; i++)
        {
        	string input = embeddingCreateDto.GetInputs()[i];

        	EmbeddingDto embedding = new EmbeddingDto() {
				Index = i,
				Embedding = _embeddingService.GenerateEmbedding(embeddingCreateDto.Model, input)
        	};

        	embeddingsDto.Data.Add(embedding);
        }

    	return Ok(embeddingsDto);
    }
}

