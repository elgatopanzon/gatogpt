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

	public ErrorDto(string message = "", string type = "", string param = "", string code = "")
	{
		Message = message;
		Type = type;
		Param = param;
		Code = code;
	}
}

public partial class ErrorObjectDto
{
	public ErrorDto Error { get; set; }

	public ErrorObjectDto(string message = "", string type = "", string param = "", string code = "")
	{
		Error = new();
		Error.Message = message;
		Error.Type = type;
		Error.Param = param;
		Error.Code = code;
	}
}

public partial class InvalidRequestErrorDto : ErrorObjectDto
{
	public InvalidRequestErrorDto(string message = "", string type = "", string param = "", string code = "") : base(message:message, type:"invalid_request_error", param:param, code:code)
	{
	}
}
