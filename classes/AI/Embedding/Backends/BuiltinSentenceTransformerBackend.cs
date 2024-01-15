/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : BuiltinSentenceTransformer
 * @created     : Friday Jan 12, 2024 21:52:50 CST
 */

namespace GatoGPT.AI.Embedding.Backends;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using AllMiniLmL6V2Sharp;
using AllMiniLmL6V2Sharp.Tokenizer;

public partial class BuiltinSentenceTransformerBackend : ModelBackend, IEmbeddingBackend
{
	public new EmbeddingModelDefinition ModelDefinition { get; set; }
	private BertTokenizer _tokenizer;
	private AllMiniLmL6V2Embedder _onnxEmbedder;

	public BuiltinSentenceTransformerBackend(EmbeddingModelDefinition modelDefinition, bool isStateful = false) : base(modelDefinition, isStateful)
	{
		ModelDefinition = modelDefinition;
		_state.Enter();
	}

	public float[] GenerateEmbedding(string input)
	{
		LoadModel();

		float[] embedding = _onnxEmbedder.GenerateEmbedding(input).ToArray();

		UnloadModel();

		return embedding;
	}

	public override void LoadModel()
	{
		// extract the paths and expected vocab.txt in same model path
        string modelPath = ModelDefinition.ModelResource.Definition.Path;
        string modelVocabPath = modelPath.Replace("/"+modelPath.GetFile(), "")+"/vocab.txt";

        LoggerManager.LogDebug("Embeddings model path", "", "modelPath", modelPath);
        LoggerManager.LogDebug("Embeddings model vocab", "", "modelVocabPath", modelVocabPath);

		_tokenizer = new BertTokenizer(modelVocabPath);
		_onnxEmbedder = new AllMiniLmL6V2Embedder(modelPath: modelPath, tokenizer: _tokenizer);
	}

	public override void UnloadModel()
	{
		_tokenizer = null;
		_onnxEmbedder = null;

		GC.Collect();
	}
}
