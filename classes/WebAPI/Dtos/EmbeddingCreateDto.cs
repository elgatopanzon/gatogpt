/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : EmbeddingCreateDto
 * @created     : Friday Jan 05, 2024 18:56:19 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class EmbeddingCreateDto
{
	public object Input { get; set; }
	public string Model { get; set; }
	public string EncodingFormat { get; set; }

	public EmbeddingCreateDto()
	{
		EncodingFormat = "float";
	}

	public List<string> GetInputs()
	{
		if (Input is string s)
		{
			return new List<string>() {s};
		}

		return (List<string>) Input;
	}
}

