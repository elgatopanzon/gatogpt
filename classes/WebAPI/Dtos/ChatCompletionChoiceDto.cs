/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionChoiceDto
 * @created     : Friday Jan 05, 2024 12:44:22 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using GatoGPT.LLM;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionChoiceDto : CompletionChoiceBaseDto
{
	public ChatCompletionMessageDto Message { get; set; }
}
