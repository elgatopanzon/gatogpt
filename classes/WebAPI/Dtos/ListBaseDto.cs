/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ListBaseDto
 * @created     : Thursday Jan 04, 2024 22:18:49 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ListBaseDto<T> : BaseDto where T : BaseDto
{
	public List<T> Data { get; set; }
}

