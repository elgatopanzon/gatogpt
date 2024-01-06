/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionCreateFunctionDto
 * @created     : Saturday Jan 06, 2024 00:52:47 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Newtonsoft.Json;

public partial class ChatCompletionCreateFunctionDto
{
	public string Description { get; set; }
	public string Name { get; set; }
	public object Parameters { get; set; }

	public ChatCompletionCreateFunctionDto()
	{
		Description = "";
		Parameters = new();
	}

	public ChatCompletionCreateFunctionParametersDto GetParametersDto()
	{
		return JsonConvert.DeserializeObject<ChatCompletionCreateFunctionParametersDto>(Parameters.ToString());
	}
}

public partial class ChatCompletionCreateFunctionParametersDto
{
	public string Type { get; set; }
	public Dictionary<string, ChatCompletionCreateFunctionPropertyDto> Properties { get; set; }
	public List<string> Required { get; set; }

	public ChatCompletionCreateFunctionParametersDto()
	{
		Properties = new();
		Required = new();
	}
}

public partial class ChatCompletionCreateFunctionPropertyDto
{
	public string Type { get; set; }
	public string Description { get; set; }
	public List<string> Enum { get; set; }

	public ChatCompletionCreateFunctionPropertyDto()
	{
		Type = "string";
		Description = "";
		Enum = new();
	}
}
