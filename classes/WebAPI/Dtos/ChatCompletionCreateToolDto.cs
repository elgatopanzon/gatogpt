/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionCreateToolDto
 * @created     : Saturday Jan 06, 2024 00:51:41 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionCreateToolDto
{
	public string Type { get; set; }
	public ChatCompletionCreateFunctionDto Function { get; set; }

	public ChatCompletionCreateToolDto()
	{
		Function = new();
	}
}

