/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CodeTesting
 * @created     : Saturday Jan 06, 2024 20:18:48 CST
 */

namespace GatoGPT;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CodeTesting
{
	private string[] _args { get; set; }

	public CodeTesting(string[] args)
	{
		LoggerManager.LogDebug("Testing class!");
	}
}

