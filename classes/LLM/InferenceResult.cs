/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : InferenceResult
 * @created     : Tuesday Jan 02, 2024 17:56:14 CST
 */

namespace GatoGPT.LLM;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class InferenceResult
{
	public int GenerationTokenCount { 
		get {
			return Tokens.Count;
		}
	}

	public int PromptTokenCount { get; set; }

	public int TotalTokenCount { 
		get {
			return GenerationTokenCount + PromptTokenCount;
		}
	}

	public List<string> Tokens { get; set; }

	public TimeSpan TimeToFirstToken {
		get {
			return FirstTokenTime - StartTime;
		}
	}

	public TimeSpan GenerationTime {
		get {
			return PrevTokenTime - StartTime;
		}
	}

	public double TokensPerSec {
		get {
			return TotalTokenCount / GenerationTime.TotalSeconds;
		}
	}

	public DateTime StartTime { get; set; }
	public DateTime EndTime { 
		get {
			return PrevTokenTime;
		}
	}
	public DateTime FirstTokenTime { get; set; }
	public DateTime PrevTokenTime { get; set; }

	public string Output { 
		get {
			return String.Join("", Tokens);
		}
	}

	public string OutputStripped { 
		get {
			return Output.Trim();
		}
	}

	public bool Finished { get; set; }

	public InferenceResult()
	{
		Tokens = new();

		StartTime = DateTime.Now;
	}

	public void AddToken(string token)
	{
		Tokens.Add(token);
	}
}

