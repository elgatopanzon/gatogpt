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

public partial class CompletionCreateDto : CompletionCreateBaseDto
{
	public object Prompt { get; set; }
	public int BestOf { get; set; }
	public string Suffix { get; set; }

	public CompletionCreateDto()
	{
		Prompt = new();
		BestOf = 1;
		Suffix = "";
	}
}

