/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionDto
 * @created     : Friday Jan 05, 2024 12:51:26 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionDto : CompletionBaseDto<ChatCompletionChoiceDto>
{
	public ChatCompletionDto()
	{
		Object = "chat.completion";
	}
}
