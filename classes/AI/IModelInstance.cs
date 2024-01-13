/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : IModelInstance
 * @created     : Friday Jan 12, 2024 18:39:12 CST
 */

namespace GatoGPT.AI;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial interface IModelInstance
{
	public string InstanceId { get; set; }
	public bool Stateful { get; set; }
	public bool IsFirstRun { get; set; }
	public bool Running { get; set; }
	public AI.ModelDefinition ModelDefinition { get; set; }

	public void LoadModel();
	public void UnloadModel();
	public bool SafeToUnloadModel();
	public void DeleteInstanceState(bool keepCache);
	public void SetInstanceId(string id, bool keepState = true);
}
