/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionChoiceDto
 * @created     : Friday Jan 05, 2024 00:18:23 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionChoiceDto : CompletionChoiceBaseDto
{
	public string Text { get; set; }
}

