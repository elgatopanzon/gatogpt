/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaCacheManagerConfig
 * @created     : Wednesday Jan 10, 2024 17:46:44 CST
 */

namespace GatoGPT.Config;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using GodotEGP.Objects.Validated;

public partial class LlamaCacheManagerConfig : VConfig
{
	internal readonly VValue<long> _maxCacheSizeMb;

	public long MaxCacheSizeMb
	{
		get { return _maxCacheSizeMb.Value; }
		set { _maxCacheSizeMb.Value = value; }
	}

	internal readonly VValue<int> _maxCacheAgeMin;

	public int MaxCacheAgeMin
	{
		get { return _maxCacheAgeMin.Value; }
		set { _maxCacheAgeMin.Value = value; }
	}

	internal readonly VValue<int> _cacheTimoutSec;

	public int CacheTimeoutSec
	{
		get { return _cacheTimoutSec.Value; }
		set { _cacheTimoutSec.Value = value; }
	}


	public LlamaCacheManagerConfig()
	{
		_maxCacheSizeMb = AddValidatedValue<long>(this)
		    .Default(1000)
		    .ChangeEventsEnabled();

		_maxCacheAgeMin = AddValidatedValue<int>(this)
		    .Default(72 * 60)
		    .ChangeEventsEnabled();

		_cacheTimoutSec = AddValidatedValue<int>(this)
		    .Default(600)
		    .ChangeEventsEnabled();
	}
}

