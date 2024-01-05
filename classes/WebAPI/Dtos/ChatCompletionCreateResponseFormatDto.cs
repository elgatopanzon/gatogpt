/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionCreateResponseFormatDto
 * @created     : Friday Jan 05, 2024 13:08:30 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionCreateResponseFormatDto
{
	public string Type { get; set; }

	public ChatCompletionCreateResponseFormatDto()
	{
		Type = "text";
	}
}

