/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LlamaCacheService
 * @created     : Wednesday Jan 10, 2024 15:15:44 CST
 */

namespace GatoGPT.Service;

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
	private long _maxCachesSizeMb { get; set; } = 10000; // TODO: add this to a global deployable config
	private int _maxCacheAgeMin { get; set; } = 60;
	private int _cacheCleanTimeout { get; set; } = 60;

	public LlamaCacheService()
	{
		
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
		_SetServiceReady(true);
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

		var cleanupTimer = new Timer();
		cleanupTimer.WaitTime = _cacheCleanTimeout;
		cleanupTimer.Autostart = true;
		cleanupTimer.OneShot = false;
		cleanupTimer.SubscribeSignal(StringNames.Instance["timeout"], false, _On_CleanupTimer_Timeout);

		AddChild(cleanupTimer);
	}

	public void _On_CleanupTimer_Timeout(IEvent e)
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

				if ((DateTime.Now - cache.CreationTime).TotalMinutes > _maxCacheAgeMin)
				{
					allCaches.Add(cache);
				}
			}
		}

		LoggerManager.LogDebug("Total cache size", "", "totalCacheSize", $"{totalCacheSizeMb}, maxCacheSize:{_maxCachesSizeMb}");
		LoggerManager.LogDebug("Caches eligable for cleaning", "", "total", allCaches.Count);

		// cleanup caches until cache size is below the max
		if (totalCacheSizeMb > _maxCachesSizeMb)
		{
			foreach(var c in allCaches.OrderBy(p => p.CreationTime).ToArray())
			{
				LoggerManager.LogDebug("Purging cache dir", "", "cacheDir", $"{c.ToString()}, CreationTime:{c.CreationTime}");

				totalCacheSizeMb -= GetDirectoryTotalSize(c);
				Directory.Delete(c.ToString(), true);

				if (totalCacheSizeMb < _maxCachesSizeMb)
				{
					break;
				}
			}
		}
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

