/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionCreateOpenAIDto
 * @created     : Sunday Jan 14, 2024 21:26:08 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Newtonsoft.Json;

public partial class ChatCompletionCreateOpenAIDto : ChatCompletionCreateOpenAIBaseDto
{
	public ChatCompletionCreateResponseFormatDto ResponseFormat {
		get {
			return BaseDto.ResponseFormat;
		}
	}
	public List<ChatCompletionMessageCreateOpenAIDto> Messages {
		get {
			List<ChatCompletionMessageCreateOpenAIDto> messages = new();

			foreach (var mes in BaseDto.Messages)
			{
				messages.Add(new() {
					Role = mes.Role,
					Name = (mes.Name.Length > 0 ? mes.Name : mes.Role),
					Content = mes.Content,
				});
			}

			return messages;
		}
	}
	[JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
	public List<ChatCompletionCreateToolDto>? Tools {
		get {
			return (BaseDto.Tools.Count > 0 ? BaseDto.Tools : null);
		}
	}
	[JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
	public object? ToolChoice {
		get {
			return (BaseDto.Tools.Count > 0 ? BaseDto.ToolChoice : null);
		}
	}

	public ChatCompletionCreateOpenAIDto(ChatCompletionCreateDto baseDto) : base(baseDto)
	{
		
	}
}

public partial class ChatCompletionMessageCreateOpenAIDto
{
	public string Role { get; set; } = "system";
	public string Name { get; set; } = "User";
	public object Content { get; set; }
}

public partial class ChatCompletionCreateOpenAIBaseDto
{
	internal ChatCompletionCreateDto BaseDto { get; set; }

	public string Model {
		get {
			return BaseDto.Model;
		}
	}
	public double FrequencyPenalty {
		get {
			return BaseDto.FrequencyPenalty;
		}
	}

	[JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
	public int? MaxTokens {
		get {
			return (BaseDto.MaxTokens < 1 ? null : BaseDto.MaxTokens);
		}
	}
	public int N {
		get {
			return BaseDto.N;
		}
	}
	public double PresencePenalty {
		get {
			return BaseDto.PresencePenalty;
		}
	}
	public int Seed {
		get {
			return BaseDto.Seed;
		}
	}
	public object Stop {
		get {
			return BaseDto.Stop;
		}
	}
	public bool Stream {
		get {
			return BaseDto.Stream;
		}
	}
	public double Temperature {
		get {
			return BaseDto.Temperature;
		}
	}
	public double TopP {
		get {
			return BaseDto.TopP;
		}
	}
	public string User {
		get {
			return BaseDto.User;
		}
	}

	public ChatCompletionCreateOpenAIBaseDto(ChatCompletionCreateDto baseDto)
	{
		BaseDto = baseDto;
	}
}
