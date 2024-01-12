/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelDto
 * @created     : Thursday Jan 04, 2024 22:08:31 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using GatoGPT.AI;
using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ModelDto : BaseDto
{
	public string Id { get; set; }
	public long Created { get; set; }
	public string OwnedBy { get; set; }

	public ModelDto()
	{
		Created = ((DateTimeOffset) DateTime.UtcNow).ToUnixTimeSeconds();
		Object = "model";
		OwnedBy = "local";
	}
}

public partial class ModelFullDto : ModelDto
{
	public LlamaModelDefinition Definition { get; set; }
}

public partial class ModelListDto : ListBaseDto<ModelDto>
{
	public ModelListDto()
	{
		Data = new();
		Object = "list";
	}
}
