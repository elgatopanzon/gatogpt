/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : Events
 * @created     : Tuesday Jan 02, 2024 14:13:40 CST
 */

namespace GatoGPT.Event;

using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class LlamaModelInstanceEvent : Event
{
	public string Id;
}
static public partial class LlamaModelInstanceEventExtensions
{
	static public T SetInstanceId<T>(this T o, string id) where T : LlamaModelInstanceEvent
	{
		o.Id = id;
		return o;
	}
}

public partial class LlamaModelLoadStart : LlamaModelInstanceEvent {};
public partial class LlamaModelLoadFinished : LlamaModelInstanceEvent {};
public partial class LlamaModelUnloadStart : LlamaModelInstanceEvent {};
public partial class LlamaModelUnloadFinished : LlamaModelInstanceEvent {};

public partial class TextGenerationInferenceStart : LlamaModelInstanceEvent {};
public partial class TextGenerationInferenceToken : LlamaModelInstanceEvent {
	public string Token;
};
public partial class TextGenerationInferenceLine : LlamaModelInstanceEvent {
	public string Line;
};
public partial class TextGenerationInferenceFinished : LlamaModelInstanceEvent {
	public InferenceResult Result;
};

static public partial class LlamaModelInstanceEventExtensions
{
	static public T SetToken<T>(this T o, string token) where T : TextGenerationInferenceToken
	{
		o.Token = token;
		return o;
	}
}
static public partial class LlamaModelInstanceEventExtensions
{
	static public T SetLine<T>(this T o, string line) where T : TextGenerationInferenceLine
	{
		o.Line = line;
		return o;
	}
}
static public partial class LlamaModelInstanceEventExtensions
{
	static public T SetResult<T>(this T o, InferenceResult result) where T : TextGenerationInferenceFinished
	{
		o.Result = result;
		return o;
	}
}


public partial class ProcessRunnerEvent : Event {}

public partial class ProcessStarted : ProcessRunnerEvent {}
public partial class ProcessOutputLine : ProcessRunnerEvent {
	public string Line { get; set; }
}
public partial class ProcessOutputErrorLine : ProcessOutputLine {}
public partial class ProcessFinishedSuccess : ProcessRunnerEvent {}
public partial class ProcessFinishedError : ProcessRunnerEvent {}
