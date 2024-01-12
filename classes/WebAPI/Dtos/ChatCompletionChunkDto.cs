/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionChunkDto
 * @created     : Sunday Jan 07, 2024 19:53:04 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionChunkDto : BaseDto
{
	public string Id { get; set; }
	public List<ChatCompletionChunkChoicesDto> Choices { get; set; }
	public long Created { get; set; }
	public string Model { get; set; }
	public string SystemFingerprint { get; set; }
	public InferenceResult InferenceResult { get; set; }

	public ChatCompletionChunkDto()
	{
		Object = "chat.completion.chunk";		
		Created = ((DateTimeOffset) DateTime.Now).ToUnixTimeSeconds();
		Choices = new();
	}
}

public partial class ChatCompletionChunkChoicesDto
{
	public int Index { get; set; }
	public ChatCompletionMessageDto Delta { get; set; }

	public ChatCompletionChunkChoicesDto()
	{
	}
}
