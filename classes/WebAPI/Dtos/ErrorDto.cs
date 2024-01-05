/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ErrorDto
 * @created     : Thursday Jan 04, 2024 23:48:22 CST
 */

namespace GatoGPT.WebAPI.Dtos;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

public partial class ErrorDto
{
	public string Message { get; set; }
	public string Type { get; set; }
	public string Param { get; set; }
	public string Code { get; set; }
}

public partial class InvalidRequestErrorDto : ErrorDto
{
	public InvalidRequestErrorDto()
	{
		Type = "invalid_request_error";
	}
}
