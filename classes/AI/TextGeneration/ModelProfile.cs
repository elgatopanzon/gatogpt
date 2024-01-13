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

public partial class ModelProfile : AI.ModelProfileBase
{
	internal readonly VNative<LoadParams> _loadParams;

	public LoadParams LoadParams
	{
		get { return _loadParams.Value; }
		set { _loadParams.Value = value; }
	}

	internal readonly VNative<InferenceParams> _inferenceParams;

	public InferenceParams InferenceParams
	{
		get { return _inferenceParams.Value; }
		set { _inferenceParams.Value = value; }
	}

	public ModelProfile()
	{
		_loadParams = AddValidatedNative<LoadParams>(this)
		    .Default(new LoadParams())
		    .ChangeEventsEnabled();

		_inferenceParams = AddValidatedNative<InferenceParams>(this)
		    .Default(new InferenceParams())
		    .ChangeEventsEnabled();
	}
}
