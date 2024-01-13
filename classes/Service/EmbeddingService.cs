/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingService
 * @created     : Friday Jan 05, 2024 22:44:51 CST
 */

namespace GatoGPT.Service;

using GatoGPT.AI.Embedding;
using GatoGPT.AI.Embedding.Backends;
using GatoGPT.Config;
using GatoGPT.Event;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Collections.Generic;

public partial class EmbeddingService : Service
{
	private EmbeddingModelManager _modelManager;

	public EmbeddingService()
	{
		_modelManager = ServiceRegistry.Get<EmbeddingModelManager>();
	}

	public float[] GenerateEmbedding(string modelDefinitionId, string input)
	{
        var modelDefinition = _modelManager.GetModelDefinition(modelDefinitionId);

		IEmbeddingBackend backend = AI.ModelBackend.CreateBackend<IEmbeddingBackend>(modelDefinition);

		return backend.GenerateEmbedding(input).ToArray();
	}

	public List<float[]> GenerateEmbeddings(string modelDefinitionId, IEnumerable<string> inputs)
	{
		List<float[]> results = new();

		foreach (var input in inputs)
		{
			results.Add(GenerateEmbedding(modelDefinitionId, input));
		}

		return results;
	}
}

