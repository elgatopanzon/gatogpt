/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionMessageCreateDto
 * @created     : Friday Jan 05, 2024 17:06:13 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Newtonsoft.Json;

public partial class ChatCompletionMessageCreateDto : ChatCompletionMessageDto
{
	[JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
	public string Name { get; set; }

	public ChatCompletionMessageCreateDto()
	{
		Name = "";
	}
}

