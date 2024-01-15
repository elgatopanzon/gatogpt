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

using Newtonsoft.Json.Linq;

public partial class ChatCompletionMessageDto
{
	public object Content { get; set; }
	public string Role { get; set; } = "system";
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
		else if (Content is List<ChatCompletionMessageContentDto> dto)
		{
			return String.Join(" ", GetContents().Where(x => x.Type == "text").Select(x => x.Text).ToArray<string>());
		}
		else
		{
			return (string) Content;
		}
	}
	public List<ChatCompletionMessageContentDto> GetContents()
	{
		List<ChatCompletionMessageContentDto> contentDtos = new();

		if (Content is Newtonsoft.Json.Linq.JArray c)
		{

			foreach (Newtonsoft.Json.Linq.JToken content in c)
			{
				IDictionary<string,object> dict = content.ToObject<Dictionary<string, object>>();

				ChatCompletionMessageContentDto contentDto = new();

				if (dict.TryGetValue("type", out var type))
				{
					contentDto.Type = (string) type;
				}
				if (dict.TryGetValue("text", out var text))
				{
					contentDto.Text = (string) text;
				}
				if (dict.TryGetValue("image_url", out var imageUrl))
				{
					var values = JObject.FromObject(imageUrl).ToObject<Dictionary<string, object>>();

					if (values.TryGetValue("url", out var url))
					{
						contentDto.ImageUrl = (string) url;
					}
				}

				contentDtos.Add(contentDto);
			}
		}
		else if (Content is List<ChatCompletionMessageContentDto> dto)
		{
			contentDtos = dto;
		}

		LoggerManager.LogDebug("Content dtos", "", "contentDtos", contentDtos);

		return contentDtos;
	}
}

public partial class ChatCompletionMessageContentDto
{
	public string Type { get; set; } = "text";
	public string Text { get; set; } = "";
	public string ImageUrl { get; set; } = "";
}
