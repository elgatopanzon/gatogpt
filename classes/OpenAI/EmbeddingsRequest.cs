/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingsRequest
 * @created     : Sunday Jan 14, 2024 18:56:14 CST
 */

namespace GatoGPT.AI.OpenAI;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using GatoGPT.WebAPI.Dtos;

public partial class EmbeddingsRequest : EmbeddingCreateDto
{
	public EmbeddingsRequest()
	{
		
	}
}

