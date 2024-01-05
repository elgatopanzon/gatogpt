/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelProfile
 * @created     : Monday Jan 01, 2024 21:04:31 CST
 */

namespace GatoGPT.LLM;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Objects.Validated;

public partial class ModelProfile : VObject
{
	internal readonly VValue<string> _name;

	public string Name
	{
		get { return _name.Value; }
		set { _name.Value = value; }
	}

	internal readonly VNative<LoadParams> _loadParams;

	public LoadParams LoadParams
	{
		get { return _loadParams.Value; }
		set { _loadParams.Value = value; }
	}

	internal readonly VNative<InferenceParams> _inferenceParams;

	public InferenceParams InferenceParams
	{
		get { return _inferenceParams.Value; }
		set { _inferenceParams.Value = value; }
	}


	public ModelProfile()
	{
		_name = AddValidatedValue<string>(this)
	    	.Default("Default Profile");

		_loadParams = AddValidatedNative<LoadParams>(this)
		    .Default(new LoadParams())
		    .ChangeEventsEnabled();

		_inferenceParams = AddValidatedNative<InferenceParams>(this)
		    .Default(new InferenceParams())
		    .ChangeEventsEnabled();
	}
}


public partial class LoadParams : VObject
{
	// token context length
	internal readonly VValue<int> _nCtx;

	public int NCtx
	{
		get { return _nCtx.Value; }
		set { _nCtx.Value = value; }
	}

	// size of tokens per batch
	internal readonly VValue<int> _nBatch;

	public int NBatch
	{
		get { return _nBatch.Value; }
		set { _nBatch.Value = value; }
	}

	// rope_freq_base
	internal readonly VValue<double> _ropeFreqBase;

	public double RopeFreqBase
	{
		get { return _ropeFreqBase.Value; }
		set { _ropeFreqBase.Value = value; }
	}

	// rope_freq_scale
	internal readonly VValue<double> _ropeFreqScale;

	public double RopeFreqScale
	{
		get { return _ropeFreqScale.Value; }
		set { _ropeFreqScale.Value = value; }
	}

	// number of layers to offload to the GPU
	internal readonly VValue<int> _nGpuLayers;

	public int NGpuLayers
	{
		get { return _nGpuLayers.Value; }
		set { _nGpuLayers.Value = value; }
	}

	// use mlock when loading the model to memory
	internal readonly VValue<bool> _useMlock;

	public bool UseMlock
	{
		get { return _useMlock.Value; }
		set { _useMlock.Value = value; }
	}

	// GPU id to use for the main model
	internal readonly VValue<int> _mainGpu;

	public int MainGpu
	{
		get { return _mainGpu.Value; }
		set { _mainGpu.Value = value; }
	}

	// seed to use for random generation
	internal readonly VValue<int> _seed;

	public int Seed
	{
		get { return _seed.Value; }
		set { _seed.Value = value; }
	}

	// whether the model should use half-precision for the key/value cache
	internal readonly VValue<bool> _f16Kv;

	public bool F16KV
	{
		get { return _f16Kv.Value; }
		set { _f16Kv.Value = value; }
	}

	// whether to load using mmap
	internal readonly VValue<bool> _useMMap;

	public bool UseMMap
	{
		get { return _useMMap.Value; }
		set { _useMMap.Value = value; }
	}


	public LoadParams()
	{
		_nCtx = AddValidatedValue<int>(this)
	    	.Default(2048)
		    .ChangeEventsEnabled();

		_nBatch = AddValidatedValue<int>(this)
	    	.Default(512)
		    .ChangeEventsEnabled();

		_ropeFreqBase = AddValidatedValue<double>(this)
		    .Default(0)
		    .ChangeEventsEnabled();

		_ropeFreqScale = AddValidatedValue<double>(this)
		    .Default(0)
		    .ChangeEventsEnabled();

		_nGpuLayers = AddValidatedValue<int>(this)
	    	.Default(0)
	    	.ChangeEventsEnabled();

		_useMlock = AddValidatedValue<bool>(this)
		    .Default(false)
		    .ChangeEventsEnabled();

		_mainGpu = AddValidatedValue<int>(this)
		    .Default(0)
		    .ChangeEventsEnabled();

		_seed = AddValidatedValue<int>(this)
		    .Default(-1)
		    .ChangeEventsEnabled();

		_f16Kv = AddValidatedValue<bool>(this)
	    	.Default(true)
	    	.ChangeEventsEnabled();

		_useMMap = AddValidatedValue<bool>(this)
		    .Default(true)
		    .ChangeEventsEnabled();
	}
}


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

		_antiprompts = AddValidatedValue<List<string>>(this)
		    .Default(new List<string>() {"### Instruction:"})
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
	}
}

