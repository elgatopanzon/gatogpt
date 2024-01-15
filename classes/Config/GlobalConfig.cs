/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : GlobalConfig
 * @created     : Sunday Jan 14, 2024 18:02:07 CST
 */

namespace GodotEGP.Config;

using GatoGPT.Config;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class GlobalConfig : VConfig
{
	internal VNative<OpenAIConfig> _openAiConfig = new();

	public OpenAIConfig OpenAIConfig
	{
		get { return _openAiConfig.Value; }
		set { _openAiConfig.Value = value; }
	}

	partial void InitConfigParams()
	{
		_openAiConfig = AddValidatedNative<OpenAIConfig>(this)
		    .Default(new OpenAIConfig())
		    .ChangeEventsEnabled();
	}
}

