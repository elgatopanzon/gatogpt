/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelDownloadConfig
 * @created     : Thursday Feb 01, 2024 23:59:05 CST
 */

namespace GatoGPT.Config;

using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ModelDownloadConfig : VConfig
{
	internal readonly VValue<List<UrlDownloadConfig>> _urlDownloads;

	public List<UrlDownloadConfig> UrlDownloads
	{
		get { return _urlDownloads.Value; }
		set { _urlDownloads.Value = value; }
	}

	internal readonly VValue<string> _downloadBasePath;

	public string DownloadBasePath
	{
		get { return _downloadBasePath.Value; }
		set { _downloadBasePath.Value = value; }
	}

	internal readonly VValue<int> _downloadProcessSec;

	public int DownloadProcessSec
	{
		get { return _downloadProcessSec.Value; }
		set { _downloadProcessSec.Value = value; }
	}

	internal readonly VValue<int> _maxConcurrentDownloads;

	public int MaxConcurrentDownloads
	{
		get { return _maxConcurrentDownloads.Value; }
		set { _maxConcurrentDownloads.Value = value; }
	}

	internal readonly VValue<long> _downloadBandwidthLimit;

	public long DownloadBandwidthLimit
	{
		get { return _downloadBandwidthLimit.Value; }
		set { _downloadBandwidthLimit.Value = value; }
	}


	public ModelDownloadConfig()
	{
		_urlDownloads = AddValidatedValue<List<UrlDownloadConfig>>(this)
		    .Default(new List<UrlDownloadConfig>())
		    .ChangeEventsEnabled();

		_downloadBasePath = AddValidatedValue<string>(this)
		    .Default("user://Models")
		    .ChangeEventsEnabled();

		_downloadProcessSec = AddValidatedValue<int>(this)
		    .Default(60)
		    .ChangeEventsEnabled();

		_maxConcurrentDownloads = AddValidatedValue<int>(this)
		    .Default(1)
		    .ChangeEventsEnabled();

		_downloadBandwidthLimit = AddValidatedValue<long>(this)
		    .Default(0)
		    .ChangeEventsEnabled();
	}
}

public partial class DownloadConfig : VConfig
{
	public string Id { get; set; }
	public string Type { get; set; }
	public string Quantization { get; set; }
	public ModelDefinition ModelDefinition { get; set; }
	public bool CreateResourceDefinition { get; set; } = true;
}

public partial class UrlDownloadConfig : DownloadConfig
{
	public string Url { get; set; }
}
