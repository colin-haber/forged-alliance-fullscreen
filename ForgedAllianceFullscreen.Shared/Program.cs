﻿using System.CommandLine;
namespace ForgedAllianceFullscreen
{
	internal class Program
	{
		static async Task<int> Main(string[] args)
		{
			var rootCommand = new RootCommand(@"Run Forged Alliance in borderless windowed mode");
			var watchOption = new Option<bool>(@"--watch", description: @"Run in the background, updating new Forged Alliance windows as they are created");
			var xOption = new Option<int>(@"--x", () => 0, description: @"The position of the left edge of the window relative to the left edge of the primary display in pixels");
			var yOption = new Option<int>(@"--y", () => 0, description: @"The position of the top of the window relative to the top of the primary display in pixels");
			var widthOption = new Option<int>(@"--width", description: @"The desired window width in pixels");
			var heightOption = new Option<int>(@"--height", description: @"The desired window height in pixels");
			var cpuOption = new Option<bool>(@"--cpu", description: @"Optimize CPU usage by setting core affinity and process priority");
			rootCommand.AddGlobalOption(watchOption);
			rootCommand.AddGlobalOption(xOption);
			rootCommand.AddGlobalOption(yOption);
			rootCommand.AddGlobalOption(widthOption);
			rootCommand.AddGlobalOption(heightOption);
			rootCommand.AddGlobalOption(cpuOption);
			rootCommand.SetHandler((watch, x, y, width, height, cpu) =>
			{
				if (watch)
				{
					new FaWindowManager(x, y, width, height, cpu).Watch().Wait();
				} else
				{
					new FaWindowManager(x, y, width, height, cpu).Run();
				}
			}, watchOption, xOption, yOption, widthOption, heightOption, cpuOption);
			return await rootCommand.InvokeAsync(args);
		}
	}
}
