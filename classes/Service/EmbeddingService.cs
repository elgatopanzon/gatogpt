/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingService
 * @created     : Friday Jan 05, 2024 22:44:51 CST
 */

namespace GatoGPT.Service;

using GatoGPT.AI.Embedding;
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

using AllMiniLmL6V2Sharp;
using AllMiniLmL6V2Sharp.Tokenizer;

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

		// extract the paths and expected vocab.txt in same model path
        string modelPath = modelDefinition.ModelResource.Definition.Path;
        string modelVocabPath = modelPath.Replace("/"+modelPath.GetFile(), "")+"/vocab.txt";

        LoggerManager.LogDebug("Embeddings model path", "", "modelPath", modelPath);
        LoggerManager.LogDebug("Embeddings model vocab", "", "modelVocabPath", modelVocabPath);

		// create custom tokenizer
		BertTokenizer tokenizer = new BertTokenizer(modelVocabPath);
		var onnxEmbedder = new AllMiniLmL6V2Embedder(modelPath: modelPath, tokenizer: tokenizer);

		float[] embedding = onnxEmbedder.GenerateEmbedding(input).ToArray();

		tokenizer = null;
		onnxEmbedder = null;

		GC.Collect();

		return embedding;
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

