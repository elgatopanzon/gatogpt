/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaLoadParams
 * @created     : Friday Jan 12, 2024 17:21:03 CST
 */

namespace GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class LlamaLoadParams : VObject
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

	internal readonly VValue<bool> _kvOffload;

	public bool KVOffload
	{
		get { return _kvOffload.Value; }
		set { _kvOffload.Value = value; }
	}


	public LlamaLoadParams()
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

		_kvOffload = AddValidatedValue<bool>(this)
		    .Default(true)
		    .ChangeEventsEnabled();
	}
}
