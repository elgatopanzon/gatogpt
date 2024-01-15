/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingBackend
 * @created     : Friday Jan 12, 2024 22:01:47 CST
 */

namespace GatoGPT.AI.Embedding.Backends;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class EmbeddingBackend : AI.ModelBackend, IEmbeddingBackend
{
	public EmbeddingBackend(EmbeddingModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		
	}

	public float[] GenerateEmbedding(string input)
	{
		return null;
	}

	public static IEmbeddingBackend CreateBackend(EmbeddingModelDefinition modelDefinition)
	{
		string fqClassName = typeof(IEmbeddingBackend).FullName;
		fqClassName = fqClassName.Replace("."+nameof(IEmbeddingBackend), "");
		fqClassName = fqClassName+"."+modelDefinition.Backend+"Backend";

		LoggerManager.LogDebug("Creating model backend instance", "", "backend", fqClassName);

		Type t = Type.GetType(fqClassName);

		if (t == null)
		{
			throw new Exception($"Invalid model backend: '{modelDefinition.Backend}'");
		}
     	return (IEmbeddingBackend) Activator.CreateInstance(t, modelDefinition);
	}
}

