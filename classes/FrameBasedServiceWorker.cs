/**
 * @author      : ElGatoPanzon (contact@elgatopanzon.io) Copyright (c) ElGatoPanzon
 * @file        : WorkerService
 * @created     : Saturday Dec 30, 2023 23:25:26 CST
 */

namespace GatoGPT;

using GodotEGP.Logging;

public partial class FrameBasedServiceWorker : BackgroundService
{
	// holds delta time values
	private DateTime _startTime = DateTime.Now;
	private DateTime _prevTime = DateTime.Now;
	private TimeSpan _deltaTime;
	private double _targetFps = 60;
	private double _frameCounter = 0;

	// the time in seconds that we want to execute the task
	private double _taskRunTimeoutSec = 5;
	private TimeSpan _taskRunCounter = new TimeSpan();

	public FrameBasedServiceWorker()
	{
        LoggerManager.LogDebug("Init background service task");
	}
        
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
    	// run initially once
        await Run();

        while (!stoppingToken.IsCancellationRequested)
        {
        	// calculate delta time
        	_deltaTime = _prevTime - _startTime;

        	if (_deltaTime.TotalMilliseconds >= (1 / _targetFps) * 1000)
        	{
        		// LoggerManager.LogDebug("Delta time", "", "deltaTime", _deltaTime);
        		// LoggerManager.LogDebug("Frame count", "", "frameCount", 1000 / _deltaTime.TotalMilliseconds);
        		_startTime = DateTime.Now;

        		_taskRunCounter += _deltaTime;

				// execute the task if timeout is reached
				if (_taskRunCounter.TotalSeconds >= _taskRunTimeoutSec)
				{
					_taskRunCounter = new TimeSpan();
            		await Run();
				}

				var _elapsedTime = DateTime.Now - _startTime;
        		// LoggerManager.LogDebug("Elapsed time", "", "elapsedTime", _elapsedTime.TotalMilliseconds);

        		// LoggerManager.LogDebug("Delta adjust", "", "deltaAdjust", _deltaTime - TimeSpan.FromMilliseconds((1 /_targetFps) * 1000));

				var tilNextFrame = TimeSpan.FromMilliseconds((1 /_targetFps) * 1000) - _elapsedTime;
        		// LoggerManager.LogDebug("Next frame in", "", "nextFrame", tilNextFrame - (_deltaTime - TimeSpan.FromMilliseconds((1 /_targetFps) * 1000)));

        		_frameCounter += 1 * _deltaTime.TotalMilliseconds / 1000;

				if (tilNextFrame.TotalMilliseconds > 0)
				{
        			await Task.Delay(tilNextFrame, stoppingToken);
				}
        	}

			// set the end frame time
            _prevTime = DateTime.Now;
        }
    }

    private Task Run()
    {
        LoggerManager.LogDebug("Executing background service task");
        LoggerManager.LogDebug("Frame count", "", "frameCount", 1000 / _deltaTime.TotalMilliseconds);
        System.Threading.Thread.Sleep(5);

        return Task.FromResult("Done");
    }

    public long GetTimeMsec()
    {
    	return DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }
}
