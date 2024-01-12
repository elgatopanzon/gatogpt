/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelProfile
 * @created     : Friday Jan 12, 2024 17:17:25 CST
 */

namespace GatoGPT.AI;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ModelProfile : VObject
{
	internal readonly VValue<string> _name;

	public string Name
	{
		get { return _name.Value; }
		set { _name.Value = value; }
	}

	public ModelProfile()
	{
		_name = AddValidatedValue<string>(this)
	    	.Default("Default Profile");

	}
}

