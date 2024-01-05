/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionCreateDto
 * @created     : Friday Jan 05, 2024 00:26:00 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionCreateDto
{
	public string Model { get; set; }
	public object Prompt { get; set; }
	public int BestOf { get; set; }
	public bool Echo { get; set; }
	public double FrequencyPenalty { get; set; }
	public int MaxTokens { get; set; }
	public int N { get; set; }
	public double PresencePenalty { get; set; }
	public double RepeatPenalty { get; set; }
	public int Seed { get; set; }
	public object Stop { get; set; }
	public bool Stream { get; set; }
	public string Suffix { get; set; }
	public double Temperature { get; set; }
	public double MinP { get; set; }
	public double TopP { get; set; }
	public int TopK { get; set; }
	public string User { get; set; }

	public CompletionCreateDto()
	{
		Prompt = new();
		BestOf = 1;
		Echo = false;
		FrequencyPenalty = 0;
		MaxTokens = 16;
		N = 1;
		PresencePenalty = 0;
		RepeatPenalty = 1.1;
		Seed = -1;
		Stop = new();
		Stream = false;
		Suffix = "";
		Temperature = 1;
		MinP = 0.05;
		TopP = 0.95;
		TopK = 40;
		User = "";
	}
}

