/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : ProcessRunner
 * @created     : Friday Jan 12, 2024 23:37:39 CST
 */

namespace GatoGPT.CLI;

using GatoGPT.Event;

using Godot;
using GodotEGP.Objects.Extensions;
using GodotEGP.Logging;
using GodotEGP.Service;
using GodotEGP.Event.Events;
using GodotEGP.Config;

using System.Diagnostics;

public partial class ProcessRunner
{
	public string _command { get; set; }
	public string[] _args { get; set; }
	public int _returnCode { get; set; }
	private ProcessStartInfo _processStartInfo;
	private Process _process;
	private TaskCompletionSource<int> _task;

	public string Output { 
		get {
			return String.Join("\n", OutputLines);
		}
	}
	public string Error { 
		get {
			return String.Join("\n", ErrorLines);
		}
	}
	public string[] OutputLines { get; set; }
	public string[] ErrorLines { get; set; }

	public ProcessRunner(string command, string[] args)
	{
		_command = command;
		_args = args;
		_task = new();

		OutputLines = new string[] {};
		ErrorLines = new string[] {};

		_processStartInfo = new ProcessStartInfo() {
			FileName = command, Arguments = String.Join(" ", _args), 
			RedirectStandardError = true,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
		};

		_process = new Process() { 
			StartInfo = _processStartInfo,
			EnableRaisingEvents = true,
		};

		_process.OutputDataReceived += _On_Process_OutputData;
		_process.ErrorDataReceived += _On_Process_ErrorData;
	}

	public void _On_Process_OutputData(object sender, DataReceivedEventArgs args)
	{
		string output = args.Data;
		if (!String.IsNullOrEmpty(args.Data))
		{
			LoggerManager.LogDebug("Process output", "", "output", output);

			OutputLines.Append(output);

			this.Emit<ProcessOutputLine>((e) => e.SetData(output));
		}
	}
	public void _On_Process_ErrorData(object sender, DataReceivedEventArgs args)
	{
		string output = args.Data;
		if (!String.IsNullOrEmpty(args.Data))
		{
			LoggerManager.LogDebug("Process output error", "", "output", output);

			ErrorLines.Append(output);

			this.Emit<ProcessOutputErrorLine>((e) => e.SetData(output));
		}
	}

	public Task<int> Execute()
	{
		LoggerManager.LogDebug("Executing process", "", "process", $"{_command} {String.Join(" ", _args)}");

		this.Emit<ProcessStarted>();

		_process.Start();
		_process.BeginErrorReadLine();
		_process.BeginOutputReadLine();

		_process.WaitForExit();

		_returnCode = _process.ExitCode;

		LoggerManager.LogDebug($"Process exited with code {_returnCode}", "", "process", $"{_command} {String.Join(" ", _args)}");
		_task.SetResult(_returnCode);
		_process.Dispose();

		if (_returnCode == 0)
		{
			ProcessExitSuccess();
		}
		else {
			ProcessExitError();
		}

		return _task.Task;
	}

	public void ProcessExitSuccess()
	{
		this.Emit<ProcessFinishedSuccess>((e) => e.SetData(_returnCode));
	}
	public void ProcessExitError()
	{
		this.Emit<ProcessFinishedError>((e) => e.SetData(_returnCode));
	}
}
