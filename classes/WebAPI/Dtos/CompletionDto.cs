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

public partial class CompletionDto : CompletionBaseDto<CompletionChoiceDto>
{
	public CompletionDto()
	{
		Object = "text_completion";
	}
}

