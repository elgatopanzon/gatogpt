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
	public object Content { get; set; }
	public string Role { get; set; }
	public List<ChatCompletionToolCallDto> ToolCalls { get; set; }
	
	public ChatCompletionMessageDto()
	{
		ToolCalls = new();
	}

	public string GetContent()
	{
		if (Content is Newtonsoft.Json.Linq.JArray)
		{
			LoggerManager.LogDebug("Contents object", "", "contents", GetContents());

			return String.Join(" ", GetContents().Where(x => x.Type == "text").Select(x => x.Text).ToArray<string>());
		}
		else
		{
			return (string) Content;
		}
	}
	public List<ChatCompletionMessageContentDto> GetContents()
	{
		List<ChatCompletionMessageContentDto> Contents = new();

		if (Content is Newtonsoft.Json.Linq.JArray c)
		{
			LoggerManager.LogDebug("TODO: map into proper dto");
		}

		return Contents;
	}
}

public partial class ChatCompletionMessageContentDto
{
	public string Type { get; set; } = "text";
	public string Text { get; set; } = "";
	public string ImageUrl { get; set; } = "";
}
