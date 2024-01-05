/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : CompletionBaseDto
 * @created     : Friday Jan 05, 2024 12:54:07 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class CompletionBaseDto<TChoiceDto> : BaseDto
{
	public string Id { get; set; }
	public List<TChoiceDto> Choices { get; set; }
	public long Created { get; set; }
	public string Model { get; set; }
	public string SystemFingerprint { get; set; }
	public CompletionUsageDto Usage { get; set; }

	public CompletionBaseDto()
	{
		Choices = new();
		Usage = new();
	}
}

