/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMConfig
 * @created     : Monday Jan 01, 2024 22:20:59 CST
 */

namespace GatoGPT.Config;

using GatoGPT.LLM;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Objects.Validated;

using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class LlamaModelPresetsConfig : VConfig
{
	// holds definitions for default model profiles
	internal readonly VValue<Dictionary<string, ModelProfile>> _defaultModelProfiles;

	public Dictionary<string, ModelProfile> DefaultModelProfiles
	{
		get { return _defaultModelProfiles.Value; }
		set { _defaultModelProfiles.Value = value; }
	}

	// map of filename matches to preset IDs
	internal readonly VValue<Dictionary<string, string>> _filenamePresetMap;

	public Dictionary<string, string> FilenamePresetMap
	{
		get { return _filenamePresetMap.Value; }
		set { _filenamePresetMap.Value = value; }
	}


	public LlamaModelPresetsConfig()
	{
		_defaultModelProfiles = AddValidatedValue<Dictionary<string, ModelProfile>>(this)
		    .Default(new Dictionary<string, ModelProfile>() {  })
		    .ChangeEventsEnabled();

		_filenamePresetMap = AddValidatedValue<Dictionary<string, string>>(this)
		    .Default(new Dictionary<string, string>())
		    .ChangeEventsEnabled();
	}

	public ModelProfile GetPresetForFilename(string filename)
	{
		foreach (var obj in FilenamePresetMap)
		{
			if (Regex.IsMatch(filename, WildCardToRegular(obj.Key)))
			{
				LoggerManager.LogDebug("Found profile matching filename", "", "match", $"{filename} = {obj.Value}");

				return GetDefaultProfile(obj.Value);
			}
		}

		return new ModelProfile();
	}

	public ModelProfile GetDefaultProfile(string profileKey)
	{
		if (DefaultModelProfiles.ContainsKey(profileKey))
		{
			return DefaultModelProfiles[profileKey];
		}

		throw new InvalidModelPreset($"The model preset {profileKey} does not exist in DefaultModelProfiles!");
	}

	public bool PresetExists(string profileKey)
	{
		return DefaultModelProfiles.ContainsKey(profileKey);
	}

	private String WildCardToRegular(String value) {
  		return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$"; 
	}

	/****************
	*  Exceptions  *
	****************/
	
	public class InvalidModelPreset : Exception
	{
		public InvalidModelPreset() { }
		public InvalidModelPreset(string message) : base(message) { }
		public InvalidModelPreset(string message, Exception inner) : base(message, inner) { }
		protected InvalidModelPreset(
			System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context)
				: base(info, context) { }
	}
}

