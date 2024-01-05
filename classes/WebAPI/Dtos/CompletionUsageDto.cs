/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionUsageDto
 * @created     : Friday Jan 05, 2024 00:17:04 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionUsageDto
{
	public int PromptTokens { get; set; }
	public int CompletionTokens { get; set; }
	public int TotalTokens { 
		get {
			return PromptTokens + CompletionTokens;
		}
	}
}

