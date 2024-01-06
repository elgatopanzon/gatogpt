/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingUsageDto
 * @created     : Friday Jan 05, 2024 18:54:28 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class EmbeddingUsageDto
{
	public int PromptTokens { get; set; }
	public int TotalTokens { 
		get {
			return PromptTokens;
		}
	}
}

