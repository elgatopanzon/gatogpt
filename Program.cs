namespace GatoGPT;

using GodotEGPNonGame.ServiceWorkers;
using GodotEGP.Logging;
using Godot;

class Program
{
	public static GodotEGP.Main GodotEGP;

    static void Main(string[] args)
    {
		var builder = WebApplication.CreateBuilder(args);

		// Add services to the container.
		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

		// background worker test
		builder.Services.AddHostedService<SceneTreeServiceWorker>();

		// init GodotEGP
		GodotEGP = new GodotEGP.Main();
		SceneTree.Instance.Root.AddChild(GodotEGP);

		var testnode = new TestNode();
		SceneTree.Instance.Root.AddChild(testnode);

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
    		app.UseSwagger();
    		app.UseSwaggerUI();
		}

		app.UseHttpsRedirection();

		var summaries = new[]
		{
    		"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		app.MapGet("/weatherforecast", () =>
		{
    		var forecast =  Enumerable.Range(1, 5).Select(index =>
        		new WeatherForecast
        		(
            		DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            		Random.Shared.Next(-20, 55),
            		summaries[Random.Shared.Next(summaries.Length)]
        		))
        		.ToArray();
    		return forecast;
		})
		.WithName("GetWeatherForecast")
		.WithOpenApi();

		app.Run();

    }
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public class TestNode : Node
{
	public override void _Ready()
	{
		LoggerManager.LogDebug("This node is ready!");
	}

	public override void _Process(double delta)
	{
		// LoggerManager.LogDebug("This node is being processed!", "", "delta", delta);
	}

	public void TestMethod()
	{
		LoggerManager.LogDebug("Test method");
	}

	public void TestCallDeferred()
	{
		CallDeferred("TestMethod");
	}
}
