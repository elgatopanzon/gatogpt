/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaModelManagerConfig
 * @created     : Tuesday Jan 02, 2024 00:48:03 CST
 */

namespace GatoGPT.Config;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using GodotEGP.Objects.Validated;

public partial class LlamaModelManagerConfig : VConfig
{
	internal readonly VValue<int> _maxThreads;

	public int MaxThreads
	{
		get { return _maxThreads.Value; }
		set { _maxThreads.Value = value; }
	}

	internal readonly VValue<int> _maxLoadedModels;

	public int MaxLoadedModels
	{
		get { return _maxLoadedModels.Value; }
		set { _maxLoadedModels.Value = value; }
	}

	internal readonly VValue<int> _modelIdleUnloadTimeout;

	public int ModelIdleUnloadTimeout
	{
		get { return _modelIdleUnloadTimeout.Value; }
		set { _modelIdleUnloadTimeout.Value = value; }
	}

	public LlamaModelManagerConfig()
	{
		_maxThreads = AddValidatedValue<int>(this)
		    .Default(-1)
		    .ChangeEventsEnabled();
		
		_maxLoadedModels = AddValidatedValue<int>(this)
		    .Default(1)
		    .ChangeEventsEnabled();

		_modelIdleUnloadTimeout = AddValidatedValue<int>(this)
		    .Default(-1)
		    .ChangeEventsEnabled();
	}
}

