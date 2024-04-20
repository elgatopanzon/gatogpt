/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMConfigHandler
 * @created     : Tuesday Jan 02, 2024 00:28:09 CST
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

public partial class LlamaConfigHandler : Handler
{
	private TextGenerationModelManager _LLMModelManager;
	private LlamaCacheService _LlamaCacheService;

	public LlamaConfigHandler()
	{
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectTypeFilter(typeof(ConfigManager)));

		// run config update when ResourceManager is ready to populate model
		// resources
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectTypeFilter(typeof(ResourceManager)));
		
		_LLMModelManager = ServiceRegistry.Get<TextGenerationModelManager>();
		_LlamaCacheService = ServiceRegistry.Get<LlamaCacheService>();
	}

	public void _On_ConfigManager_Ready(IEvent e)
	{
		// subscribe to changes on model preset and definitions config
		var sc = ServiceRegistry.Get<ConfigManager>().Get<TextGenerationModelManagerConfig>();
		sc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		var pc = ServiceRegistry.Get<ConfigManager>().Get<TextGenerationPresetsConfig>();
		pc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		var dc = ServiceRegistry.Get<ConfigManager>().Get<ModelDefinitionsConfig>();
		dc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		// trigger changed event
		_On_ModelsConfig_Changed(sc, pc, dc);

		// subscribe to LlamaCacheManagerConfig
		var cm = ServiceRegistry.Get<ConfigManager>().Get<LlamaCacheManagerConfig>();
		cm.SubscribeOwner<ValidatedValueChanged>(_On_CacheConfig_ValueChanged, isHighPriority: true);

		_On_CacheConfig_Changed(cm);
	}

	public void _On_ModelsConfig_ValueChanged(IEvent e)
	{
		var sc = ServiceRegistry.Get<ConfigManager>().Get<TextGenerationModelManagerConfig>();
		var pc = ServiceRegistry.Get<ConfigManager>().Get<TextGenerationPresetsConfig>();
		var dc = ServiceRegistry.Get<ConfigManager>().Get<ModelDefinitionsConfig>();

		_On_ModelsConfig_Changed(sc, pc, dc);
	}

	public void _On_ModelsConfig_Changed(TextGenerationModelManagerConfig managerConfig, TextGenerationPresetsConfig presetsConfig, ModelDefinitionsConfig definitionsConfig)
	{
		_LLMModelManager.SetConfig(managerConfig, presetsConfig, definitionsConfig);

		_LLMModelManager.SetModelResources(ServiceRegistry.Get<ResourceManager>().GetResources<LlamaModel>());
	}

	public void _On_CacheConfig_ValueChanged(IEvent e)
	{
		var cm = ServiceRegistry.Get<ConfigManager>().Get<LlamaCacheManagerConfig>();

		_On_CacheConfig_Changed(cm);
	}

	public void _On_CacheConfig_Changed(LlamaCacheManagerConfig cm)
	{
		_LlamaCacheService.SetConfig(cm);
	}
}

