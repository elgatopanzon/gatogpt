/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionDto
 * @created     : Friday Jan 05, 2024 00:14:54 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionDto : BaseDto
{
	public string Id { get; set; }
	public List<CompletionChoiceDto> Choices { get; set; }
	public long Created { get; set; }
	public string Model { get; set; }
	public string SystemFingerprint { get; set; }
	public CompletionUsageDto Usage { get; set; }

	public CompletionDto()
	{
		Object = "text_completion";
		Choices = new();
		Usage = new();
	}
}

