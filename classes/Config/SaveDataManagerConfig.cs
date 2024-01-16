/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : SaveDataManagerConfig
 * @created     : Monday Jan 15, 2024 18:58:03 CST
 */

namespace GodotEGP.Config;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class SaveDataManagerConfig : VObject
{
	partial void InitConfigParams()
	{
		// disable creation of System data
		AutocreateSystemData = false;
	}
}
