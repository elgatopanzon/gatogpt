/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : InferenceResult
 * @created     : Tuesday Jan 02, 2024 17:56:14 CST
 */

namespace GatoGPT.AI.TextGeneration;

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

	internal List<string> Tokens { get; set; }

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
			return GenerationTokenCount / (GenerationTime.TotalSeconds - TimeToFirstToken.TotalSeconds);
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

	public string OutputStripped { get; set; }

	internal bool Finished { get; set; }

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

