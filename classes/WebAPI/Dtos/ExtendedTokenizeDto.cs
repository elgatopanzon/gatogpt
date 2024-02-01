/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ExtendedTokenizeDto
 * @created     : Wednesday Jan 31, 2024 17:39:56 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ExtendedTokenizeDto
{
	public List<TokenizedString> Tokens { get; set; }
}

