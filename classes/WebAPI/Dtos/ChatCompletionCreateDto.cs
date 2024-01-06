/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionCreateDto
 * @created     : Friday Jan 05, 2024 13:07:04 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionCreateDto : CompletionCreateBaseDto
{
	public ChatCompletionCreateResponseFormatDto ResponseFormat { get; set; }
	public List<ChatCompletionMessageCreateDto> Messages { get; set; }
	public List<ChatCompletionCreateToolDto> Tools { get; set; }
	public object ToolChoice { get; set; }

	public ChatCompletionCreateDto()
	{
		ResponseFormat = new();
		Tools = new();
		ToolChoice = new();
	}

	public string GetToolChoice()
	{
		if (ToolChoice is string ts)
			return ts;
		else
			return "";
	}

	public ChatCompletionToolChoiceDto GetToolChoiceObject()
	{
		return (ChatCompletionToolChoiceDto) ToolChoice;
	}
}

