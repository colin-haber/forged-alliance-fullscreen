using System.CommandLine;
namespace ForgedAllianceFullscreen
{
	internal class Program
	{
		static async Task<int> Main(string[] args)
		{
			var rootCommand = new RootCommand(@"Run Forged Alliance in borderless windowed mode");
			var watchCommand = new Command(@"--watch", description: @"Run in the background, updating new Forged Alliance windows as they are created");
			var xOption = new Option<int>(@"--x", () => 0, description: @"The position of the left edge of the window relative to the left edge of the primary display in pixels");
			var yOption = new Option<int>(@"--y", () => 0, description: @"The position of the top of the window relative to the top of the primary display in pixels");
			var widthOption = new Option<int>(@"--width", description: @"The desired window width in pixels");
			var heightOption = new Option<int>(@"--height", description: @"The desired window height in pixels");
			rootCommand.AddCommand(watchCommand);
			rootCommand.AddGlobalOption(xOption);
			rootCommand.AddGlobalOption(yOption);
			rootCommand.AddGlobalOption(widthOption);
			rootCommand.AddGlobalOption(heightOption);
			rootCommand.SetHandler((x, y, width, height) =>
			{
				new FaWindowManager(x, y, width, height).Run();
			}, xOption, yOption, widthOption, heightOption);
			watchCommand.SetHandler((x, y, width, height) =>
			{
				new FaWindowManager(x, y, width, height).Watch().Wait();
			}, xOption, yOption, widthOption, heightOption);
			
			return await rootCommand.InvokeAsync(args);
		}
	}
}
