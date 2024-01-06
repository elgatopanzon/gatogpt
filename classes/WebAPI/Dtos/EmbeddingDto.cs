/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingDto
 * @created     : Friday Jan 05, 2024 18:49:46 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class EmbeddingsDto : ListBaseDto<EmbeddingDto>
{
	public string Model { get; set; }
	public EmbeddingUsageDto Usage { get; set; }

	public EmbeddingsDto()
	{
		Object = "list";
		Data = new();
	}
}

public partial class EmbeddingDto : BaseDto
{
	public float[] Embedding { get; set; }
	public int Index { get; set; }

	public EmbeddingDto()
	{
		Object = "embedding";
		Index = 0;
	}
}
