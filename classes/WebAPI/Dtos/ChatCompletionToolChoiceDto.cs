/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionToolChoiceDto
 * @created     : Saturday Jan 06, 2024 00:59:47 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionToolChoiceDto
{
	public string Type { get; set; }
	public ChatCompletionToolChoiceFunctionDto Function { get; set; }

	public ChatCompletionToolChoiceDto()
	{
		Function = new();
	}
}

public partial class ChatCompletionToolChoiceFunctionDto
{
	public string Name { get; set; }
}
