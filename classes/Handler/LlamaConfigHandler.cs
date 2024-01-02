/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMConfigHandler
 * @created     : Tuesday Jan 02, 2024 00:28:09 CST
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

public partial class LlamaConfigHandler : Handler
{
	private LlamaModelManager _LLMModelManager;

	public LlamaConfigHandler()
	{
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectType(typeof(ConfigManager)));

		// run config update when ResourceManager is ready to populate model
		// resources
		ServiceRegistry.Get<EventManager>().Subscribe<ServiceReady>(_On_ConfigManager_Ready).Filters(new OwnerObjectType(typeof(ResourceManager)));
		
		_LLMModelManager = ServiceRegistry.Get<LlamaModelManager>();
	}

	public void _On_ConfigManager_Ready(IEvent e)
	{
		// subscribe to changes on model preset and definitions config
		var sc = ServiceRegistry.Get<ConfigManager>().Get<LlamaModelManagerConfig>();
		sc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		var pc = ServiceRegistry.Get<ConfigManager>().Get<LlamaModelPresetsConfig>();
		pc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		var dc = ServiceRegistry.Get<ConfigManager>().Get<LlamaModelDefinitionsConfig>();
		dc.SubscribeOwner<ValidatedValueChanged>(_On_ModelsConfig_ValueChanged, isHighPriority: true);

		// trigger changed event
		_On_ModelsConfig_Changed(sc, pc, dc);
	}

	public void _On_ModelsConfig_ValueChanged(IEvent e)
	{
		var sc = ServiceRegistry.Get<ConfigManager>().Get<LlamaModelManagerConfig>();
		var pc = ServiceRegistry.Get<ConfigManager>().Get<LlamaModelPresetsConfig>();
		var dc = ServiceRegistry.Get<ConfigManager>().Get<LlamaModelDefinitionsConfig>();

		_On_ModelsConfig_Changed(sc, pc, dc);
	}

	public void _On_ModelsConfig_Changed(LlamaModelManagerConfig managerConfig, LlamaModelPresetsConfig presetsConfig, LlamaModelDefinitionsConfig definitionsConfig)
	{
		_LLMModelManager.SetConfig(managerConfig, presetsConfig, definitionsConfig);

		_LLMModelManager.SetModelResources(ServiceRegistry.Get<ResourceManager>().GetResources<LlamaModel>());
	}
}

