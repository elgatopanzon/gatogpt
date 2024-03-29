/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : IModelInstance
 * @created     : Friday Jan 12, 2024 18:40:09 CST
 */

namespace GatoGPT.AI.TextGeneration.Backends;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial interface ITextGenerationBackend : AI.IModelBackend
{
	public void StartInference(string promptText, AI.TextGeneration.LoadParams loadParams = null, AI.TextGeneration.InferenceParams inferenceParams = null);

	public new AI.TextGeneration.ModelDefinition ModelDefinition { get; set; }
	public AI.TextGeneration.LoadParams LoadParams { get; set; }
	public AI.TextGeneration.InferenceParams InferenceParams { get; set; }
	public string Prompt { get; set; }
	public string CurrentInferenceLine { get; set; }
	public InferenceResult InferenceResult { get; set; }
	public Dictionary<string, (string Type, string Value)> Metadata { get; set; }

	public bool Persistent { get; set; }

	public bool Finished
	{
		get { 
			if (InferenceResult != null)
			{
				return InferenceResult.Finished;
			}
			else
			{
				return false;
			}
		}
	}
	public abstract List<TokenizedString> TokenizeString(string content, bool skipBos = true);
	public abstract string FormatPrompt(string prompt);
}

