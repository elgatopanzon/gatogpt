/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionCreateOpenAIDto
 * @created     : Sunday Jan 14, 2024 21:07:22 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionCreateOpenAIDto : CompletionCreateOpenAIBaseDto
{
	public object Prompt { 
		get {
			return BaseDto.Prompt;
		}
	}
	public int BestOf {
		get {
			return BaseDto.BestOf;
		}
	}
	public string Suffix {
		get {
			return BaseDto.Suffix;
		}
	}

	public CompletionCreateOpenAIDto(CompletionCreateDto baseDto) : base(baseDto)
	{
		
	}
}

public partial class CompletionCreateOpenAIBaseDto
{
	internal CompletionCreateDto BaseDto { get; set; }

	public string Model {
		get {
			return BaseDto.Model;
		}
	}
	public bool Echo {
		get {
			return BaseDto.Echo;
		}
	}
	public double FrequencyPenalty {
		get {
			return BaseDto.FrequencyPenalty;
		}
	}
	public int MaxTokens {
		get {
			return BaseDto.MaxTokens;
		}
	}
	public int N {
		get {
			return BaseDto.N;
		}
	}
	public double PresencePenalty {
		get {
			return BaseDto.PresencePenalty;
		}
	}
	public int Seed {
		get {
			return BaseDto.Seed;
		}
	}
	public object Stop {
		get {
			return BaseDto.Stop;
		}
	}
	public bool Stream {
		get {
			return BaseDto.Stream;
		}
	}
	public double Temperature {
		get {
			return BaseDto.Temperature;
		}
	}
	public double TopP {
		get {
			return BaseDto.TopP;
		}
	}
	public string User {
		get {
			return BaseDto.User;
		}
	}

	public CompletionCreateOpenAIBaseDto(CompletionCreateDto baseDto)
	{
		BaseDto = baseDto;
	}
}

