/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : LLMConfig
 * @created     : Monday Jan 01, 2024 22:20:59 CST
 */

namespace GatoGPT.Config;

using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;
using GodotEGP.Objects.Validated;

using System.Collections.Generic;
using System.Text.RegularExpressions;

public partial class TextGenerationPresetsConfig : VConfig
{
	// holds definitions for default model profiles
	internal readonly VValue<Dictionary<string, LlamaModelProfile>> _defaultModelProfiles;

	public Dictionary<string, LlamaModelProfile> DefaultModelProfiles
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


	public TextGenerationPresetsConfig()
	{
		_defaultModelProfiles = AddValidatedValue<Dictionary<string, LlamaModelProfile>>(this)
		    .Default(new Dictionary<string, LlamaModelProfile>() {  })
		    .ChangeEventsEnabled();

		_filenamePresetMap = AddValidatedValue<Dictionary<string, string>>(this)
		    .Default(new Dictionary<string, string>())
		    .ChangeEventsEnabled();
	}

	public LlamaModelProfile GetPresetForFilename(string filename)
	{
		foreach (var obj in FilenamePresetMap)
		{
			if (Regex.IsMatch(filename, WildCardToRegular(obj.Key)))
			{
				LoggerManager.LogDebug("Found profile matching filename", "", "match", $"{filename} = {obj.Value}");

				return GetDefaultProfile(obj.Value);
			}
		}

		return new LlamaModelProfile();
	}

	public LlamaModelProfile GetDefaultProfile(string profileKey)
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

