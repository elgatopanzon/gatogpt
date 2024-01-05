/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionChoiceDto
 * @created     : Friday Jan 05, 2024 00:18:23 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using GatoGPT.LLM;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionChoiceDto
{
	public string FinishReason { get; set; }
	public int Index { get; set; }
	public object Logprobs { get; set; } // TODO
	public string Text { get; set; }
	public InferenceResult InferenceResult { get; set; }
}

