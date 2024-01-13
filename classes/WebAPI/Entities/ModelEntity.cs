/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelEntity
 * @created     : Thursday Jan 04, 2024 22:06:35 CST
 */

namespace GatoGPT.WebAPI.Entities;

using GatoGPT.AI.TextGeneration;
using GatoGPT.AI;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ModelEntity
{
	public AI.ModelDefinition Model;

	public string Id
	{
		get { return Model.Id; }
	}
	public string OwnedBy
	{
		get { return Model.OwnedBy; }
	}
}

