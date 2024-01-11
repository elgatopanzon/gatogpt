/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaCacheService
 * @created     : Wednesday Jan 10, 2024 15:15:44 CST
 */

namespace GatoGPT.Service;

using GatoGPT.Config;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Misc;

public partial class LlamaCacheService : Service
{
	private string _cacheBaseDir { get; set; } = Path.Combine(OS.GetUserDataDir(), "Cache");

	private LlamaCacheManagerConfig _config { get; set; }

	private Timer _cleanupTimer;

	public LlamaCacheService()
	{
		_cleanupTimer = new Timer();
	}

	public void SetConfig(LlamaCacheManagerConfig config)
	{
		LoggerManager.LogDebug("Setting cache config", "", "config", config);

		_config = config;

		_cleanupTimer.WaitTime = _config.CacheTimeoutSec;

		if (!GetReady())
		{
			_SetServiceReady(true);
		}

		_cleanupTimer.Stop();
		_cleanupTimer.Start(_config.CacheTimeoutSec);

		Cleanup();
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	// Called when service is registered in manager
	public override void _OnServiceRegistered()
	{
	}

	// Called when service is deregistered from manager
	public override void _OnServiceDeregistered()
	{
		// LoggerManager.LogDebug($"Service deregistered!", "", "service", this.GetType().Name);
	}

	// Called when service is considered ready
	public override void _OnServiceReady()
	{
		// setup a Timer node to run the cleanup process
		LoggerManager.LogDebug("Setting up cleanup timer");

		_cleanupTimer.WaitTime = _config.CacheTimeoutSec;
		_cleanupTimer.Autostart = true;
		_cleanupTimer.OneShot = false;
		_cleanupTimer.SubscribeSignal(StringNames.Instance["timeout"], false, _On_CleanupTimer_Timeout);

		AddChild(_cleanupTimer);
	}

	public void Cleanup()
	{
		if (!Directory.Exists(_cacheBaseDir))
		{
			return;
		}

		DirectoryInfo cacheIdDir = new DirectoryInfo (_cacheBaseDir);

		DirectoryInfo[] cacheIdDirs = cacheIdDir.GetDirectories().OrderBy(p => p.CreationTime).ToArray();

		long totalCacheSizeMb = 0;

		List<DirectoryInfo> allCaches = new();

		foreach (DirectoryInfo cacheDir in cacheIdDirs)
		{
			DirectoryInfo[] caches = cacheDir.GetDirectories().OrderBy(p => p.CreationTime).ToArray();

			foreach (DirectoryInfo cache in caches)
			{
				long cacheSize = GetDirectoryTotalSize(cache);
				totalCacheSizeMb += cacheSize;

				if ((DateTime.Now - cache.CreationTime).TotalMinutes > _config.MaxCacheAgeMin)
				{
					allCaches.Add(cache);
				}
			}
		}

		if (allCaches.Count > 0)
		{
			LoggerManager.LogDebug("Caches eligable for cleaning", "", "total", allCaches.Count);
			LoggerManager.LogDebug("Total cache size", "", "totalCacheSize", $"{totalCacheSizeMb}, maxCacheSize:{_config.MaxCacheSizeMb}");

			// cleanup caches until cache size is below the max
			if (totalCacheSizeMb > _config.MaxCacheSizeMb)
			{
				foreach(var c in allCaches.OrderBy(p => p.CreationTime).ToArray())
				{
					LoggerManager.LogDebug("Purging cache dir", "", "cacheDir", $"{c.ToString()}, CreationTime:{c.CreationTime}");

					totalCacheSizeMb -= GetDirectoryTotalSize(c);
					Directory.Delete(c.ToString(), true);

					if (totalCacheSizeMb < _config.MaxCacheSizeMb)
					{
						break;
					}
				}
			}
		}
	}

	public void _On_CleanupTimer_Timeout(IEvent e)
	{
		Cleanup();
	}

	public long BytesToMb(long bytes)
	{
		return bytes / 1000000;
	}

	public long GetDirectoryTotalSize(DirectoryInfo dir)
	{
		return BytesToMb(dir.EnumerateFiles().Sum(file => file.Length));
	}
}

