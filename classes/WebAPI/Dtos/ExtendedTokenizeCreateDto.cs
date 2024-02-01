/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ExtendedTokenizeCreateDto
 * @created     : Wednesday Jan 31, 2024 17:39:33 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ExtendedTokenizeCreateDto
{
	public string Model { get; set; }
	public string Content { get; set; }
}
