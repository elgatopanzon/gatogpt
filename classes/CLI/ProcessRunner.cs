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
using GodotEGP.Threading;
using System.ComponentModel;

using System.Diagnostics;

public partial class ProcessRunner : BackgroundJob
{
	public string Command { get; set; }
	public string[] Args { get; set; }
	public int ReturnCode { get; set; } = -1;

	public bool Success {
		get {
			return ReturnCode == 0;
		}
	}


	private ProcessStartInfo _processStartInfo;
	private Process _process;
	private TaskCompletionSource<int> _task;

	public string Output { 
		get {
			return String.Join("\n", OutputLines);
		}
	}
	public string OutputStripped { 
		get {
			return Output.Trim();
		}
	}
	public string Error { 
		get {
			return String.Join("\n", ErrorLines);
		}
	}
	public string ErrorStripped { 
		get {
			return Error.Trim();
		}
	}
	public List<string> OutputLines { get; set; }
	public List<string> ErrorLines { get; set; }

	public List<ProcessOutputFilter> OutputFilters { get; set; } = new();

	public DateTime StartTime { get; set; } = DateTime.Now;
	public DateTime EndTime { get; set; } = DateTime.Now;
	public TimeSpan ExecutionTime {
		get {
			return EndTime - StartTime;
		}
	}

	public ProcessRunner(string command, params string[] args)
	{
		Command = command;
		Args = args;
		_task = new();

		OutputLines = new();
		ErrorLines = new();
	}

	public void AddArguments(params string[] args)
	{
		Args = Args.Concat(args).ToArray();
	}

	public void AddOutputFilter(Func<string, bool> func)
	{
		OutputFilters.Add(new ProcessOutputFilter(func));
	}

	public bool GetOutputFilterMatch(string output)
	{
		bool match = false;

		foreach (var filter in OutputFilters)
		{
			if (filter.Run(output))
			{
				match = true;
			}
		}

		return match;
	}

	public async Task<int> Execute()
	{
		_processStartInfo = new ProcessStartInfo() {
			FileName = Command,
			Arguments = String.Join(" ", Args), 
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

		Run();

		return await _task.Task;
	}

	public void ProcessExitSuccess()
	{
		this.Emit<ProcessFinishedSuccess>((e) => e.SetData(ReturnCode));
	}
	public void ProcessExitError()
	{
		this.Emit<ProcessFinishedError>((e) => e.SetData(ReturnCode));
	}

	public override void DoWork(object sender, DoWorkEventArgs e)
	{
		LoggerManager.LogDebug("Executing process", "", "process", $"{Command} {String.Join(" ", Args)}");

		this.Emit<ProcessStarted>();

		_process.Start();
		_process.BeginErrorReadLine();
		_process.BeginOutputReadLine();

		_process.WaitForExit();

		ReturnCode = _process.ExitCode;

		LoggerManager.LogDebug($"Process exited with code {ReturnCode}", "", "process", $"{Command} {String.Join(" ", Args)}");
		_process.Dispose();

		EndTime = DateTime.Now;

		e.Result = ReturnCode;
	}

	public override void ProgressChanged(object sender, ProgressChangedEventArgs e)
	{
	}

	public void _On_Process_OutputData(object sender, DataReceivedEventArgs args)
	{
		string output = args.Data;
		if (!String.IsNullOrEmpty(args.Data))
		{
			if (GetOutputFilterMatch(output))
			{
				LoggerManager.LogDebug("Output filter match, excluding", "", "excludedOutput", output);
				return;
			}

			LoggerManager.LogDebug("Process output", "", "output", output);

			OutputLines.Add(output);

			this.Emit<ProcessOutputLine>((e) => e.Line = output);
		}
	}
	public void _On_Process_ErrorData(object sender, DataReceivedEventArgs args)
	{
		string output = args.Data;
		if (!String.IsNullOrEmpty(args.Data))
		{
			LoggerManager.LogDebug("Process output error", "", "output", output);

			ErrorLines.Add(output);

			this.Emit<ProcessOutputErrorLine>((e) => e.Line = output);
		}
	}


	public override void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
	{
		if (ReturnCode == 0)
		{
			ProcessExitSuccess();
		}
		else {
			ProcessExitError();
		}

		_task.SetResult(ReturnCode);
	}

	public override void RunWorkerError(object sender, RunWorkerCompletedEventArgs e)
	{
		ProcessExitError();

		_task.SetResult(ReturnCode);
	}
}

public partial class ProcessOutputFilter
{
	public Func<string, bool> Func { get; set; }

	public ProcessOutputFilter(Func<string, bool> func)
	{
		Func = func;
	}

	public bool Run(string output)
	{
		return Func(output);
	}
}
