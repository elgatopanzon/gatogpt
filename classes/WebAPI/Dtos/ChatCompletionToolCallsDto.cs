/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionToolCallsDto
 * @created     : Saturday Jan 06, 2024 01:15:14 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionToolCallDto
{
	public string Id { get; set; }
	public string Type { get; set; }
	public ChatCompletionToolFunctionDto Function { get; set; }

	public ChatCompletionToolCallDto()
	{
		Function = new();
	}
}

public partial class ChatCompletionToolCallRawDto
{
	public string Function { get; set; }
	public Dictionary<string, object> Arguments { get; set; }

	public ChatCompletionToolCallRawDto()
	{
		Arguments = new();
	}
}

