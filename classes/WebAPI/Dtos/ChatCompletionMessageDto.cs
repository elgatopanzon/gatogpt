/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionMessageDto
 * @created     : Friday Jan 05, 2024 12:47:45 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionMessageDto
{
	public string Content { get; set; }
	public string Role { get; set; }
	public List<ChatCompletionToolCallDto> ToolCalls { get; set; }
	
	public ChatCompletionMessageDto()
	{
		ToolCalls = new();
	}
}

