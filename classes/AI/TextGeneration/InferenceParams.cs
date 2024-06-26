/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaInferenceParams
 * @created     : Friday Jan 12, 2024 17:21:42 CST
 */

namespace GatoGPT.AI.TextGeneration;

using GatoGPT.Resource;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Resource;

public partial class InferenceParams : VObject
{
	// number of threads to use for inference
	internal readonly VValue<int> _n_threads;

	public int NThreads
	{
		get { return _n_threads.Value; }
		set { _n_threads.Value = value; }
	}

	internal readonly VValue<int> _keepTokens;

	public int KeepTokens
	{
		get { return _keepTokens.Value; }
		set { _keepTokens.Value = value; }
	}

	// number of tokens to generate
	internal readonly VValue<int> _nPredict;

	public int NPredict
	{
		get { return _nPredict.Value; }
		set { _nPredict.Value = value; }
	}

	// top_k: reduce risk of generating low tokens
	internal readonly VValue<int> _topK;

	public int TopK
	{
		get { return _topK.Value; }
		set { _topK.Value = value; }
	}

	// probability for tokens
	internal readonly VValue<double> _minP;

	public double MinP
	{
		get { return _minP.Value; }
		set { _minP.Value = value; }
	}

	internal readonly VValue<double> _topP;

	public double TopP
	{
		get { return _topP.Value; }
		set { _topP.Value = value; }
	}

	// temp, controls randomness
	internal readonly VValue<double> _temp;

	public double Temp
	{
		get { return _temp.Value; }
		set { _temp.Value = value; }
	}

	// penalty for repeating tokens
	internal readonly VValue<double> _repeatPenalty;

	public double RepeatPenalty
	{
		get { return _repeatPenalty.Value; }
		set { _repeatPenalty.Value = value; }
	}

	// penalty for frequency
	internal readonly VValue<double> _frequencyPenalty;

	public double FrequencyPenalty
	{
		get { return _frequencyPenalty.Value; }
		set { _frequencyPenalty.Value = value; }
	}

	// penalty for presence
	internal readonly VValue<double> _presencePenalty;

	public double PresencePenalty
	{
		get { return _presencePenalty.Value; }
		set { _presencePenalty.Value = value; }
	}

	internal readonly VValue<double> _tfs;

	public double Tfs
	{
		get { return _tfs.Value; }
		set { _tfs.Value = value; }
	}

	internal readonly VValue<double> _typical;

	public double Typical
	{
		get { return _typical.Value; }
		set { _typical.Value = value; }
	}

	internal readonly VValue<double> _mirostat;

	public double Mirostat
	{
		get { return _mirostat.Value; }
		set { _mirostat.Value = value; }
	}

	internal readonly VValue<double> _mirostatLearningRate;

	public double MirostatLearningRate
	{
		get { return _mirostatLearningRate.Value; }
		set { _mirostatLearningRate.Value = value; }
	}

	internal readonly VValue<double> _MirostatEntropy;

	public double MirostatEntropy
	{
		get { return _MirostatEntropy.Value; }
		set { _MirostatEntropy.Value = value; }
	}

	// samplers config for --samplers
	internal readonly VValue<List<string>> _samplers;

	public List<string> Samplers
	{
		get { return _samplers.Value; }
		set { _samplers.Value = value; }
	}

	internal readonly VValue<int> _repeatLastN;

	public int RepeatLastN
	{
		get { return _repeatLastN.Value; }
		set { _repeatLastN.Value = value; }
	}

	// prompt and input related config
	internal readonly VValue<List<string>> _antiprompts;

	public List<string> Antiprompts
	{
		get { return _antiprompts.Value; }
		set { _antiprompts.Value = value; }
	}

	// input prefix and suffix to send to the model
	internal readonly VValue<string> _inputPrefix;

	public string InputPrefix
	{
		get { return _inputPrefix.Value; }
		set { _inputPrefix.Value = value; }
	}

	internal readonly VValue<string> _inputSuffix;

	public string InputSuffix
	{
		get { return _inputSuffix.Value; }
		set { _inputSuffix.Value = value; }
	}

	// system/pre prompt
	internal readonly VValue<string> _prePrompt;

	public string PrePrompt
	{
		get { return _prePrompt.Value; }
		set { _prePrompt.Value = value; }
	}

	internal readonly VValue<string> _prePromptPrefix;

	public string PrePromptPrefix
	{
		get { return _prePromptPrefix.Value; }
		set { _prePromptPrefix.Value = value; }
	}

	internal readonly VValue<string> _prePromptSuffix;

	public string PrePromptSuffix
	{
		get { return _prePromptSuffix.Value; }
		set { _prePromptSuffix.Value = value; }
	}

	// multimodal --image param
	internal readonly VValue<string> _imagePath;

	public string ImagePath
	{
		get { return _imagePath.Value; }
		set { _imagePath.Value = value; }
	}

	internal readonly VValue<string> _imageModelId;

	public string ImageModelId
	{
		get { return _imageModelId.Value; }
		set { _imageModelId.Value = value; }
	}

	internal readonly VValue<string> _instructTemplate;

	public string InstructTemplate
	{
		get { return _instructTemplate.Value; }
		set { _instructTemplate.Value = value; }
	}

	internal readonly VValue<string> _chatTemplate;

	public string ChatTemplate
	{
		get { return _chatTemplate.Value; }
		set { _chatTemplate.Value = value; }
	}

	internal readonly VValue<string> _chatMessageTemplate;

	public string ChatMessageTemplate
	{
		get { return _chatMessageTemplate.Value; }
		set { _chatMessageTemplate.Value = value; }
	}

	internal readonly VValue<string> _chatMessageGenerationTemplate;

	public string ChatMessageGenerationTemplate
	{
		get { return _chatMessageGenerationTemplate.Value; }
		set { _chatMessageGenerationTemplate.Value = value; }
	}

	internal readonly VValue<string> _templateType;

	public string TemplateType
	{
		get { return _templateType.Value; }
		set { _templateType.Value = value; }
	}

	internal readonly VValue<string> _GrammaResourceId;

	public string GrammarResourceId
	{
		get { return _GrammaResourceId.Value; }
		set { _GrammaResourceId.Value = value; }
	}

	internal readonly VValue<ResourceObject<LlamaGrammar>> _grammarResource;

	internal ResourceObject<LlamaGrammar> GrammarResource
	{
		get { return _grammarResource.Value; }
		set { _grammarResource.Value = value; }
	}

	internal readonly VValue<string> _negativeCfgPrompt;

	public string NegativeCfgPrompt
	{
		get { return _negativeCfgPrompt.Value; }
		set { _negativeCfgPrompt.Value = value; }
	}

	internal readonly VValue<double> _cfgScale;

	public double CfgScale
	{
		get { return _cfgScale.Value; }
		set { _cfgScale.Value = value; }
	}

	internal readonly VValue<string> _promptCacheId;

	public string PromptCacheId
	{
		get { return _promptCacheId.Value; }
		set { _promptCacheId.Value = value; }
	}

	public InferenceParams()
	{
		_n_threads = AddValidatedValue<int>(this)
	    	.Default(0)
		    .ChangeEventsEnabled();

		_keepTokens = AddValidatedValue<int>(this)
		    .Default(0)
		    .ChangeEventsEnabled();

		_nPredict = AddValidatedValue<int>(this)
		    .Default(-1)
		    .ChangeEventsEnabled();

		_topK = AddValidatedValue<int>(this)
		    .Default(40)
		    .ChangeEventsEnabled();

		_minP = AddValidatedValue<double>(this)
		    .Default(0.05)
		    .ChangeEventsEnabled();

		_topP = AddValidatedValue<double>(this)
		    .Default(0.95)
		    .ChangeEventsEnabled();

		_temp = AddValidatedValue<double>(this)
		    .Default(0.8)
		    .ChangeEventsEnabled();

		_repeatPenalty = AddValidatedValue<double>(this)
		    .Default(1.1)
		    .ChangeEventsEnabled();

		_frequencyPenalty = AddValidatedValue<double>(this)
		    .Default(0)
		    .ChangeEventsEnabled();

		_presencePenalty = AddValidatedValue<double>(this)
		    .Default(0)
		    .ChangeEventsEnabled();

		_tfs = AddValidatedValue<double>(this)
		    .Default(1.0)
		    .ChangeEventsEnabled();

		_typical = AddValidatedValue<double>(this)
		    .Default(1.0)
		    .ChangeEventsEnabled();

		_mirostat = AddValidatedValue<double>(this)
		    .Default(0)
		    .ChangeEventsEnabled();

		_mirostatLearningRate = AddValidatedValue<double>(this)
		    .Default(0.1)
		    .ChangeEventsEnabled();

		_MirostatEntropy = AddValidatedValue<double>(this)
		    .Default(5.0)
		    .ChangeEventsEnabled();

		_samplers = AddValidatedValue<List<string>>(this)
		    .Default(new List<string>() { "top_k", "tfs", "typical", "top_p", "min_p", "temp" })
		    .ChangeEventsEnabled();

		_repeatLastN = AddValidatedValue<int>(this)
		    .Default(64)
		    .ChangeEventsEnabled();

		_antiprompts = AddValidatedValue<List<string>>(this)
		    .Default(new List<string>() {})
		    .ChangeEventsEnabled();

		_inputPrefix = AddValidatedValue<string>(this)
		    .Default("### Instruction:\n")
		    .ChangeEventsEnabled();

		_inputSuffix = AddValidatedValue<string>(this)
		    .Default("\n### Response:\n")
		    .ChangeEventsEnabled();

		_prePrompt = AddValidatedValue<string>(this)
		    .Default("Below is an instruction that describes a task. Write a response that appropriately completes the request.")
		    .ChangeEventsEnabled();

		_prePromptPrefix = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		_prePromptSuffix = AddValidatedValue<string>(this)
		    .Default("\n")
		    .ChangeEventsEnabled();

		_imagePath = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		_imageModelId = AddValidatedValue<string>(this)
		    .Default("image")
		    .ChangeEventsEnabled();

		_instructTemplate = AddValidatedValue<string>(this)
		    .Default("{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ InputPrefix }}{{ Input }}{{ InputSuffix }}")
		    .ChangeEventsEnabled();

		_chatTemplate = AddValidatedValue<string>(this)
		    .Default("{{ PrePromptPrefix }}{{ PrePrompt }}{{ PrePromptSuffix }}{{ InputPrefix }}{{ Input }}{{ InputSuffix }}")
		    .ChangeEventsEnabled();

		_chatMessageTemplate = AddValidatedValue<string>(this)
		    .Default("{{ Name }}: {{ Message }}")
		    .ChangeEventsEnabled();

		_chatMessageGenerationTemplate = AddValidatedValue<string>(this)
		    .Default("{{ AssistantName }}: ")
		    .ChangeEventsEnabled();

		_templateType = AddValidatedValue<string>(this)
		    .Default("instruct")
		    .ChangeEventsEnabled();

		_GrammaResourceId = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		_grammarResource = AddValidatedValue<ResourceObject<LlamaGrammar>>(this)
		    .Default(null)
		    .ChangeEventsEnabled();

		_negativeCfgPrompt = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();

		_cfgScale = AddValidatedValue<double>(this)
		    .Default(1.0)
		    .ChangeEventsEnabled();

		_promptCacheId = AddValidatedValue<string>(this)
		    .Default("")
		    .ChangeEventsEnabled();
	}
}
