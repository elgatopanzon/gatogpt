/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : DownloadConfigHandler
 * @created     : Friday Feb 02, 2024 00:16:24 CST
 */

namespace GatoGPT.Handler;

using GatoGPT.Config;
using GatoGPT.AI.TextGeneration;
using GatoGPT.Service;
using GatoGPT.Resource;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Event.Filters;
using GodotEGP.Config;
using GodotEGP.Handler;

public partial class DownloadConfigHandler : Handler
{
	private ModelDownloadManager _modelDownloadManager;

	public DownloadConfigHandler()
	{
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectTypeFilter(typeof(ConfigManager)));

		// run config update when ResourceManager is ready to populate model
		// resources
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectTypeFilter(typeof(ResourceManager)));

		_modelDownloadManager = ServiceRegistry.Get<ModelDownloadManager>();
	}

	public void _On_ConfigManager_Ready(IEvent e)
	{
		// subscribe to changes on model download config
		var mdc = ServiceRegistry.Get<ConfigManager>().Get<ModelDownloadConfig>();
		mdc.SubscribeOwner<ValidatedValueChanged>(_On_ModelDownloadConfig_ValueChanged, isHighPriority: true);

		// trigger changed event
		_On_ModelDownloadConfig_Changed(mdc);
	}

	public void _On_ModelDownloadConfig_ValueChanged(IEvent e)
	{
		var mdc = ServiceRegistry.Get<ConfigManager>().Get<ModelDownloadConfig>();

		_On_ModelDownloadConfig_Changed(mdc);
	}

	public void _On_ModelDownloadConfig_Changed(ModelDownloadConfig mdc)
	{
		_modelDownloadManager.SetConfig(mdc);

		_modelDownloadManager.SetResourceDefinitions(ServiceRegistry.Get<ConfigManager>().Get<ResourceDefinitionConfig>());
	}
}

