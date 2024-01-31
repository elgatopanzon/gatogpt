/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ITokenFilter
 * @created     : Tuesday Jan 30, 2024 19:55:48 CST
 */

namespace GatoGPT.AI.TextGeneration;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial interface ITokenFilter
{
	public bool Match(string[] tokens, string[] allTokens);
	public string[] Filter(string[] tokens, string[] allTokens);
}

