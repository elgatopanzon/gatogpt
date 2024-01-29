/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ChatCompletionCreateExtendedDto
 * @created     : Sunday Jan 28, 2024 20:43:57 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ChatCompletionCreateExtendedDto
{
	public ChatCompletionCreateExtendedModelDto Model { get; set; }
	public ChatCompletionCreateExtendedInferenceDto Inference { get; set; }
}

public partial class ChatCompletionCreateExtendedModelDto
{
	public int? NCtx { get; set; }
	public int? NBatch { get; set; }
	public int? NGpuLayers { get; set; }
	public string? Backend { get; set; }
	public bool? PromptCache { get; set; }
}
public partial class ChatCompletionCreateExtendedInferenceDto
{
	public int? NThreads { get; set; }
	public int? NKeep { get; set; }
	public int? TopK { get; set; }
	public double? Tfs { get; set; }
	public double? Typical { get; set; }
	public double? RepeatPenalty { get; set; }
	public int? RepeatLastN { get; set; }
	public bool? Vision { get; set; }
	public string? GrammarResourceId { get; set; }
}
