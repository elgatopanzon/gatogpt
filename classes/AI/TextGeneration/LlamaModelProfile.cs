/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelProfile
 * @created     : Monday Jan 01, 2024 21:04:31 CST
 */

namespace GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Objects.Validated;

public partial class LlamaModelProfile : ModelProfile
{
	internal readonly VNative<LlamaLoadParams> _loadParams;

	public LlamaLoadParams LoadParams
	{
		get { return _loadParams.Value; }
		set { _loadParams.Value = value; }
	}

	internal readonly VNative<LlamaInferenceParams> _inferenceParams;

	public LlamaInferenceParams InferenceParams
	{
		get { return _inferenceParams.Value; }
		set { _inferenceParams.Value = value; }
	}

	public LlamaModelProfile()
	{
		_loadParams = AddValidatedNative<LlamaLoadParams>(this)
		    .Default(new LlamaLoadParams())
		    .ChangeEventsEnabled();

		_inferenceParams = AddValidatedNative<LlamaInferenceParams>(this)
		    .Default(new LlamaInferenceParams())
		    .ChangeEventsEnabled();
	}
}
