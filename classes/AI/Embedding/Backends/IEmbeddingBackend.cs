/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : IEmbeddingBackend
 * @created     : Friday Jan 12, 2024 21:48:58 CST
 */

namespace GatoGPT.AI.Embedding.Backends;

using GatoGPT.AI;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial interface IEmbeddingBackend : AI.IModelBackend
{
	public float[] GenerateEmbedding(string input);
}
