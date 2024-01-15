/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : OpenAIConfig
 * @created     : Sunday Jan 14, 2024 17:56:00 CST
 */

namespace GatoGPT.Config;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Objects.Validated;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class OpenAIConfig : VConfig
{
	internal readonly VValue<string> _apiKey;

	public string APIKey
	{
		get { return _apiKey.Value; }
		set { _apiKey.Value = value; }
	}

	internal readonly VValue<string> _apiHostname;

	public string APIHostname
	{
		get { return _apiHostname.Value; }
		set { _apiHostname.Value = value; }
	}

	internal readonly VValue<int> _apiPort;

	public int APIPort
	{
		get { return _apiPort.Value; }
		set { _apiPort.Value = value; }
	}

	internal readonly VValue<bool> _apiUseSsl;

	public bool APIUseSSL
	{
		get { return _apiUseSsl.Value; }
		set { _apiUseSsl.Value = value; }
	}

	internal string Host
	{
		get { return $"{(APIUseSSL ? "https://" : "http://")}{APIHostname}:{APIPort}"; }
	}

	public OpenAIConfig()
	{
		_apiKey = AddValidatedValue<string>(this)
		    .Default("no-key")
		    .ChangeEventsEnabled();

		_apiHostname = AddValidatedValue<string>(this)
		    .Default("api.openai.com")
		    .ChangeEventsEnabled();

		_apiPort = AddValidatedValue<int>(this)
		    .Default(443)
		    .ChangeEventsEnabled();

		_apiUseSsl = AddValidatedValue<bool>(this)
		    .Default(true)
		    .ChangeEventsEnabled();
	}
}

