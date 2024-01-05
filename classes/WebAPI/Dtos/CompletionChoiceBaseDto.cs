/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionChoiceBaseDto
 * @created     : Friday Jan 05, 2024 12:45:37 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using GatoGPT.LLM;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionChoiceBaseDto
{
	public string FinishReason { get; set; }
	public int Index { get; set; }
	public object Logprobs { get; set; } // TODO
	public InferenceResult InferenceResult { get; set; }
}

