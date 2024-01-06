/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingsConfigHandler
 * @created     : Friday Jan 05, 2024 23:39:39 CST
 */

namespace GatoGPT.Handler;

using GatoGPT.Config;
using GatoGPT.LLM;
using GatoGPT.Service;
using GatoGPT.Resource;

using Godot;
using GodotEGP;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Event.Filter;
using GodotEGP.Config;
using GodotEGP.Handler;

public partial class EmbeddingConfigHandler : Handler
{
	private EmbeddingModelManager _embeddingsManager;

	public EmbeddingConfigHandler()
	{
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectType(typeof(ConfigManager)));

		// run config update when ResourceManager is ready to populate model
		// resources
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectType(typeof(ResourceManager)));
		
		_embeddingsManager = ServiceRegistry.Get<EmbeddingModelManager>();
	}

	public void _On_ConfigManager_Ready(IEvent e)
	{
		// subscribe to changes on model preset and definitions config
		var sc = ServiceRegistry.Get<ConfigManager>().Get<EmbeddingModelManagerConfig>();
		sc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		// var pc = ServiceRegistry.Get<ConfigManager>().Get<EmbeddingModelPresetsConfig>();
		// pc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		var dc = ServiceRegistry.Get<ConfigManager>().Get<EmbeddingModelDefinitionsConfig>();
		dc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		// trigger changed event
		_On_ModelsConfig_Changed(sc, dc);
	}

	public void _On_ModelsConfig_ValueChanged(IEvent e)
	{
		var sc = ServiceRegistry.Get<ConfigManager>().Get<EmbeddingModelManagerConfig>();
		// var pc = ServiceRegistry.Get<ConfigManager>().Get<EmbeddingModelPresetsConfig>();
		var dc = ServiceRegistry.Get<ConfigManager>().Get<EmbeddingModelDefinitionsConfig>();

		_On_ModelsConfig_Changed(sc, dc);
	}

	public void _On_ModelsConfig_Changed(EmbeddingModelManagerConfig managerConfig, EmbeddingModelDefinitionsConfig definitionsConfig)
	{
		_embeddingsManager.SetConfig(managerConfig, definitionsConfig);

		_embeddingsManager.SetModelResources(ServiceRegistry.Get<ResourceManager>().GetResources<EmbeddingModel>());
	}
}

