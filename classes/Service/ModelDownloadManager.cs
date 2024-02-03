/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ModelDownloadManager
 * @created     : Thursday Feb 01, 2024 23:57:03 CST
 */

namespace GatoGPT.Service;

using GatoGPT.Config;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Resource;
using GodotEGP.Data;
using GodotEGP.Data.Endpoint;
using GodotEGP.Data.Operation;
using GodotEGP.Misc;

using System.Net;
using System.ComponentModel;

public partial class ModelDownloadManager : Service
{
	private ModelDownloadConfig _config { get; set; }
	private TextGenerationModelManager _textgenModelManager { get; set; }
	private ResourceDefinitionConfig _resourceDefinitions { get; set; }
	private ConfigManager _configManager { get; set; }

	private string _resourceCategory = "DownloadedModels";

	// download operations
	private Dictionary<(HTTPEndpoint Http, FileEndpoint File), (DataOperationProcessRemoteTransfer<Resource<GodotEGP.Resource.RemoteTransferResult>> Process, UrlDownloadConfig Config)> _downloadOperations { get; set; } = new();

	private Timer _downloadTimer;

	public ModelDownloadManager()
	{
		_config = new();

		_downloadTimer = new Timer();

		_textgenModelManager = ServiceRegistry.Get<TextGenerationModelManager>();
		_configManager = ServiceRegistry.Get<ConfigManager>();
	}

	public void SetConfig(ModelDownloadConfig config)
	{
		_config = config;

		_downloadTimer.WaitTime = _config.DownloadProcessSec;

		LoggerManager.LogDebug("Setting config", "", "config", config);

		if (!GetReady())
		{
			_SetServiceReady(true);
		}

		if (!_downloadTimer.IsStopped())
		{
			_downloadTimer.Stop();
			_downloadTimer.Start(_config.DownloadProcessSec);
		}
	}

	public void SetResourceDefinitions(ResourceDefinitionConfig resourceDefinitions)
	{
		_resourceDefinitions = resourceDefinitions;
	}

	/*******************
	 *  Godot methods  *
	 *******************/

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/*********************
	 *  Service methods  *
	 *********************/

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
		_downloadTimer.WaitTime = _config.DownloadProcessSec;
		_downloadTimer.Autostart = true;
		_downloadTimer.OneShot = false;
		_downloadTimer.SubscribeSignal(StringNames.Instance["timeout"], false, _On_DownloadTimer_Timeout);

		AddChild(_downloadTimer);
	}

	/*********************************
	*  Download management methods  *
	*********************************/

	public void _On_DownloadTimer_Timeout(IEvent e)
	{
		ProcessDownloads();
	}

	public void ProcessDownloads()
	{
		// go over each type of download and queue them up in a
		// RemoteTransferOperation
		int activeDownloads = GetActiveDownloadCount();

		// url downloads
		foreach (var download in _config.UrlDownloads)
		{
			var endpoints = GetDownloadEndpoints(download);

			if (GetExistingDownloadProcess(endpoints.Http, endpoints.File).Process == null && !File.Exists(endpoints.File.Path))
			{
				LoggerManager.LogDebug("Creating download process", "", "download", download);
				// create download process operation
				var process = new DataOperationProcessRemoteTransfer<Resource<GodotEGP.Resource.RemoteTransferResult>>(endpoints.File, endpoints.Http,
						onErrorCb: _On_DownloadOperation_Error, 
						onProgressCb: _On_DownloadOperation_Progress, 
						onCompleteCb: _On_DownloadOperation_Complete
					);

				_downloadOperations.Add(endpoints, (process, download));
			}
		}

		// start download processes
		foreach (var operation in _downloadOperations)
		{
			// skip working downloads and finished downloads
			if (operation.Value.Process.DataOperation.Working || File.Exists(operation.Key.File.Path))
			{
				continue;
			}

			if (activeDownloads < _config.MaxConcurrentDownloads)
			{
				LoggerManager.LogDebug("Starting download operation", "", "download", operation.Value.Config);

				activeDownloads++;

				try
				{
					// trigger async save, we can listen for the result via
					// events
					operation.Value.Process.SaveAsync();
				}
				catch (System.Exception e)
				{
					LoggerManager.LogDebug("Error during download", "", "error", e.Message);
				}
			}
		}
	}

	public void ProcessFinishedDownload(RemoteTransferResult remoteTransferResult)
	{
		LoggerManager.LogDebug("Processing completed download", "", "downloadResult", remoteTransferResult);

		var process = GetExistingDownloadProcess(remoteTransferResult.HTTPEndpoint, remoteTransferResult.FileEndpoint);

		if (process.Process != null)
		{
			LoggerManager.LogDebug("Download config for result", "", "downloadConfig", process.Config);

			if (process.Config.CreateResourceDefinition)
			{
				GodotEGP.Resource.Definition resourceDefinition = new() {
					Path = remoteTransferResult.FileEndpoint.Path,
					Class = "GatoGPT.Resource.LlamaModel",
				};
				resourceDefinition.Path = resourceDefinition.Path.Replace(ProjectSettings.GlobalizePath(_config.DownloadBasePath), _config.DownloadBasePath);

				if (!_resourceDefinitions.Resources.TryGetValue(_resourceCategory, out var modelDefinitions))
				{
					modelDefinitions = new();	
					_resourceDefinitions.Resources.Add(_resourceCategory, modelDefinitions);
				}

					LoggerManager.LogDebug("Adding resource definition for model", "", "definition", resourceDefinition);

				string resourceId = process.Config.Id+"/"+process.Config.Quantization;
				modelDefinitions.TryAdd(resourceId, resourceDefinition);

				// create a new config object to store downloaded resource
				// definitions
				GodotEGP.Config.Object<ResourceDefinitionConfig> modelResourcesConfig = new();
				modelResourcesConfig.Value = new();

				modelResourcesConfig.Value.Resources[_resourceCategory] = new();

				// add resources from download category into new config object
				foreach (var resource in _resourceDefinitions.Resources[_resourceCategory])
				{
					modelResourcesConfig.Value.Resources[_resourceCategory].Add(resource.Key, resource.Value);
				}

				// create endpoint for resource configs
				modelResourcesConfig.DataEndpoint = _configManager.GetDefaultSaveEndpoint(typeof(ResourceDefinitionConfig), $"{_resourceCategory}.json");

				LoggerManager.LogDebug("Saving downloaded resource definitions", "", "resourceDefinitions", modelResourcesConfig);

				modelResourcesConfig.Save();

				// create a model definition and save it to DownloadedModels.json
				if (process.Config.ModelDefinition.Id != null)
				{
					// create a new config object to store downloaded model
					// definitions
					GodotEGP.Config.Object<ModelDefinitionsConfig> modelDefinitionsConfig = new();
					modelDefinitionsConfig.Value = new();

					// add resources from download category into new config object
					foreach (var download in _config.UrlDownloads)
					{
						if (download.ModelDefinition.Id != null)
						{
							modelDefinitionsConfig.Value.TextGeneration[download.ModelDefinition.Id] = new(download.Id+"/"+download.Quantization);
						}
					}

					// create endpoint for resource configs
					modelDefinitionsConfig.DataEndpoint = _configManager.GetDefaultSaveEndpoint(typeof(ModelDefinitionsConfig), $"{_resourceCategory}.json");

					LoggerManager.LogDebug("Saving downloaded model definitions", "", "resourceDefinitions", modelDefinitionsConfig);

					modelDefinitionsConfig.Save();
				}
			}

		}
		else
		{
			LoggerManager.LogDebug("Process not found for download result", "", "downloadResult", remoteTransferResult);
		}

		ProcessDownloads();
	}

	public int GetActiveDownloadCount()
	{
		int count = 0;

		foreach (var downloadProcess in _downloadOperations)
		{
			if (downloadProcess.Value.Process.DataOperation.Working)
			{
				count++;
			}
		}

		return count;
	}

	public (DataOperationProcessRemoteTransfer<Resource<GodotEGP.Resource.RemoteTransferResult>> Process, UrlDownloadConfig Config) GetExistingDownloadProcess(HTTPEndpoint httpEndpoint, FileEndpoint fileEndpoint)
	{
		foreach (var downloadProcess in _downloadOperations)
		{
			if (downloadProcess.Key.Http.Uri.AbsoluteUri == httpEndpoint.Uri.AbsoluteUri && downloadProcess.Key.File.Path == fileEndpoint.Path)
			{
				return downloadProcess.Value;
			}
		}

		return (null, null);
	}

	public string GetDownloadBasePath(UrlDownloadConfig downloadConfig)
	{
		var idSplit = downloadConfig.Id.Split("/");
		return Path.Combine($"{_config.DownloadBasePath}", idSplit[0], idSplit[1]);
	}

	public (HTTPEndpoint Http, FileEndpoint File) GetDownloadEndpoints(UrlDownloadConfig downloadConfig)
	{
		HTTPEndpoint httpEndpoint = new(new Uri(downloadConfig.Url));
		httpEndpoint.BandwidthLimit = _config.DownloadBandwidthLimit;

		FileEndpoint fileEndpoint = new(Path.Combine(GetDownloadBasePath(downloadConfig), Path.Combine(httpEndpoint.Uri.PathAndQuery.Split("?")[0].GetFile())));

		return (httpEndpoint, fileEndpoint);
	}

	/********************************
	*  Download process callbacks  *
	********************************/
	
	public void _On_DownloadOperation_Error(IEvent e)
	{
		if (e is DataOperationError ee)
		{
			LoggerManager.LogDebug("Download process error", "", "error", ee);		
		}
	}

	public void _On_DownloadOperation_Complete(IEvent e)
	{
		if (e is DataOperationComplete ee)
		{
			LoggerManager.LogDebug("Download process complete", "", "result", ee.RunWorkerCompletedEventArgs.Result);		

			var res = ee.RunWorkerCompletedEventArgs.Result;

			if (res is OperationResult<Resource<RemoteTransferResult>> resobj)
			{
				ProcessFinishedDownload(resobj.ResultObject.Value);
			}

		}
	}

	public void _On_DownloadOperation_Progress(IEvent e)
	{
		if (e is DataOperationProgress ee)
		{
			
		}
	}
}
