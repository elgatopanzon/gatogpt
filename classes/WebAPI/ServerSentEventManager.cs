/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ServerSentEventManager
 * @created     : Sunday Jan 07, 2024 19:22:08 CST
 */

namespace GatoGPT.WebAPI;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public partial class ServerSentEventManager
{
	private HttpContext _httpContext { get; set; }
	private bool _done = false;

	public ServerSentEventManager(HttpContext httpContext)
	{
		_httpContext = httpContext;
	}

	public void Start()
	{
		_httpContext.Response.Headers.Add("Content-Type", "text/event-stream");
	}

	public async Task<bool> SendEvent(object eventObject, string eventPrefix = "data: ")
	{
		if (eventPrefix.Length > 0)
		{
			await _httpContext.Response.WriteAsync(eventPrefix);
		}
		if (eventObject is string stringObj)
		{
        	await _httpContext.Response.WriteAsync(stringObj);
		}
		else
        	await _httpContext.Response.WriteAsync(JsonConvert.SerializeObject(eventObject, 
        				new JsonSerializerSettings
					{
    					ContractResolver = new DefaultContractResolver() { NamingStrategy = new SnakeCaseNamingStrategy() },
					}));
        await _httpContext.Response.WriteAsync($"\n\n");
        await _httpContext.Response.Body.FlushAsync();	

        return true;
	}

	public async Task<bool> Done()
	{
		_done = true;
		return await SendEvent("[DONE]");
	}

	public async Task<bool> WaitDone()
	{
		while (!_done)
		{
			await Task.Delay(100);
		}

		return true;
	}
}

