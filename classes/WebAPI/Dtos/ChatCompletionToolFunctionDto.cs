/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionToolFunctionDto
 * @created     : Saturday Jan 06, 2024 01:18:28 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionToolFunctionDto
{
	public string Name { get; set; }
	public string Arguments { get; set; }

	public ChatCompletionToolFunctionDto()
	{
		
	}
}

