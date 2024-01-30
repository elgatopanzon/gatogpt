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
using System.Text;

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
			return String.Join("", OutputStrings);
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
	public List<string> OutputStrings { get; set; }
	public List<string> ErrorLines { get; set; }

	public List<ProcessOutputFilter> OutputFilters { get; set; } = new();

	public DateTime StartTime { get; set; } = DateTime.Now;
	public DateTime EndTime { get; set; } = DateTime.Now;
	public TimeSpan ExecutionTime {
		get {
			return EndTime - StartTime;
		}
	}
	
	public ConsoleAutomator _automator { get; set; }

	public ProcessRunner(string command, params string[] args)
	{
		Command = command;
		Args = args;
		_task = new();

		OutputStrings = new();
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

	public void Kill()
	{
		_process.Kill(true);
	}

	public void ProcessExitSuccess()
	{
		this.Emit<ProcessFinishedSuccess>((e) => e.SetData(ReturnCode));
	}
	public void ProcessExitError()
	{
		this.Emit<ProcessFinishedError>((e) => e.SetData(ReturnCode));
	}

	public void AutomatorStandardInputRead(object sender, ConsoleInputReadEventArgs e)
	{
		ProcessConsoleOutput(e.Input);
	}

	public override void DoWork(object sender, DoWorkEventArgs e)
	{
		LoggerManager.LogDebug("Executing process", "", "process", $"{Command} {String.Join(" ", Args)}");

		this.Emit<ProcessStarted>();

		_process.Start();

		_automator = new ConsoleAutomator(_process.StandardInput, _process.StandardOutput);
		_automator.StandardInputRead += AutomatorStandardInputRead;
		_automator.StartAutomate();

		_process.BeginErrorReadLine();
		// _process.BeginOutputReadLine();

		_process.WaitForExit();
		_automator.StandardInputRead -= AutomatorStandardInputRead;

		ReturnCode = _process.ExitCode;

		LoggerManager.LogDebug($"Process exited with code {ReturnCode}", "", "process", $"{Command} {String.Join(" ", Args)}");
		_process.Dispose();

		EndTime = DateTime.Now;

		e.Result = ReturnCode;
	}

	public override void ProgressChanged(object sender, ProgressChangedEventArgs e)
	{
	}

	public void ProcessConsoleOutput(string output)
	{
		if (!String.IsNullOrEmpty(output))
		{
			if (GetOutputFilterMatch(output))
			{
				LoggerManager.LogDebug("Output filter match, excluding", "", "excludedOutput", output);
				return;
			}

			LoggerManager.LogDebug("Process output", "", "output", output);

			OutputStrings.Add(output);

			this.Emit<ProcessOutputLine>((e) => e.Line = output);
		}
	}

	public void _On_Process_OutputData(object sender, DataReceivedEventArgs args)
	{
		ProcessConsoleOutput(args.Data+"\n");
	}
	public void _On_Process_ErrorData(object sender, DataReceivedEventArgs args)
	{
		string output = args.Data;
		if (!String.IsNullOrEmpty(args.Data))
		{
			if (GetOutputFilterMatch(output))
			{
				LoggerManager.LogDebug("Error output filter match, excluding", "", "excludedErrorOutput", output);
				return;
			}

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

public class ConsoleInputReadEventArgs : EventArgs
{
    public ConsoleInputReadEventArgs(string input)
    {
        this.Input = input;
    }

    public string Input { get; private set; }
}

public interface IConsoleAutomator
{
    StreamWriter StandardInput { get; }

    event EventHandler<ConsoleInputReadEventArgs> StandardInputRead;
}

public abstract class ConsoleAutomatorBase : IConsoleAutomator
{
    protected readonly StringBuilder inputAccumulator = new StringBuilder();

    protected readonly byte[] buffer = new byte[256];

    protected volatile bool stopAutomation;

    public StreamWriter StandardInput { get; protected set; }

    protected StreamReader StandardOutput { get; set; }

    protected StreamReader StandardError { get; set; }

    public event EventHandler<ConsoleInputReadEventArgs> StandardInputRead;

    protected void BeginReadAsync()
    {
        if (!this.stopAutomation) {
            this.StandardOutput.BaseStream.BeginRead(this.buffer, 0, this.buffer.Length, this.ReadHappened, null);
        }
    }

    protected virtual void OnAutomationStopped()
    {
        this.stopAutomation = true;
        this.StandardOutput.DiscardBufferedData();
    }

    private void ReadHappened(IAsyncResult asyncResult)
    {
        var bytesRead = this.StandardOutput.BaseStream.EndRead(asyncResult);
        if (bytesRead == 0) {
            this.OnAutomationStopped();
            return;
        }

        var input = this.StandardOutput.CurrentEncoding.GetString(this.buffer, 0, bytesRead);
        this.inputAccumulator.Append(input);

        if (bytesRead < this.buffer.Length) {
            this.OnInputRead(this.inputAccumulator.ToString());
        }

        this.BeginReadAsync();
    }

    private void OnInputRead(string input)
    {
        var handler = this.StandardInputRead;
        if (handler == null) {
            return;
        }

        handler(this, new ConsoleInputReadEventArgs(input));
        this.inputAccumulator.Clear();
    }
}

public class ConsoleAutomator : ConsoleAutomatorBase, IConsoleAutomator
{
    public ConsoleAutomator(StreamWriter standardInput, StreamReader standardOutput)
    {
        this.StandardInput = standardInput;
        this.StandardOutput = standardOutput;
    }

    public void StartAutomate()
    {
        this.stopAutomation = false;
        this.BeginReadAsync();
    }

    public void StopAutomation()
    {
        this.OnAutomationStopped();
    }
}
