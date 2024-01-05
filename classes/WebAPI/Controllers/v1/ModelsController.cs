/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelsController
 * @created     : Thursday Jan 04, 2024 21:41:23 CST
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

using AutoMapper;
// using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
// using GatoGPT.WebAPI.Dtos;
// using GatoGPT.WebAPI.Entities;
using System.Text.Json;

[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/[controller]")]
public partial class ModelsController : ControllerBase
{
	private readonly IMapper _mapper;
	private readonly LlamaModelManager _modelManager;

	public ModelsController(IMapper mapper)
	{
		_mapper = mapper;
		 _modelManager = ServiceRegistry.Get<LlamaModelManager>();
	}

    [HttpGet(Name = nameof(ListModels))]
    public ActionResult<ModelListDto> ListModels(ApiVersion version)
    {
    	ModelListDto modelsDto = new();


    	// get list of mapped ModelDto objects
    	IEnumerable<ModelDto> dtos = GetModelEntities().Select(x => _mapper.Map<ModelDto>(x));

    	// assign model Dto list to modelsListDto object
    	modelsDto.Data = dtos.ToList();

        return Ok(modelsDto);
    }

    [HttpGet("{model}", Name = nameof(GetModel))]
    public ActionResult<ModelDto> GetModel(ApiVersion version, string model)
    {
    	if (_modelManager.ModelDefinitions.ContainsKey(model))
    	{
    		ModelDto modelDto = GetModelEntities().Where(x => x.Id == model).Select(x => _mapper.Map<ModelDto>(x)).FirstOrDefault();

        	return Ok(modelDto);
    	}

    	return NotFound(new InvalidRequestErrorDto() {
			Message = $"The model '{model}' does not exist",
			Code = "model_not_found",
			Param = "model",
    	});
    }

	[ApiExplorerSettings(IgnoreApi = true)]
    public List<ModelEntity> GetModelEntities()
    {
    	// convert model definitions into ModelEntity objects
    	List<ModelEntity> modelEntities = new();

    	foreach (var def in _modelManager.ModelDefinitions)
    	{
    		var entity = new ModelEntity() {
				Model = def.Value,
    		};

    		modelEntities.Add(entity);
    	}

    	return modelEntities;
    }
}

