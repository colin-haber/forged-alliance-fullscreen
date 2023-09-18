using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Timers;
using Timer = System.Timers.Timer;
namespace ForgedAllianceFullscreen
{
	internal class FaWindowManager
	{
		private const nint HWND_TOP = 0;
		private const uint SWP_SHOWWINDOW = 0x0040;
		private const uint SWP_FRAMECHANGED = 0x0020;
		private const int SW_SHOWMAXIMIZED = 3;
		private const int SW_RESTORE = 9;
		private const int GWL_STYLE = -16;
		private const long WS_BORDERLESS = 0x16000000;
		[DllImport("user32.dll")]
		private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int width, int height, uint uFlags);
		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
		[DllImport("user32.dll")]
		private static extern long GetWindowLongW(IntPtr hWnd, int nIndex);
		[DllImport("user32.dll")]
		private static extern long SetWindowLongW(IntPtr hWnd, int nIndex, long dwNewLong);
		private readonly ISet<int> fullscreenedProcessIds;
		private readonly int x;
		private readonly int y;
		private readonly int width;
		private readonly int height;
		private readonly bool cpu;
		private readonly ILogger logger;
		internal FaWindowManager(int x, int y, int width, int height, bool cpu)
		{
			using var loggerFactory = LoggerFactory.Create(builder =>
			{
				builder.AddFilter("Microsoft", LogLevel.Warning)
							 .AddFilter("System", LogLevel.Warning)
							 .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
							 .AddSimpleConsole(options =>
							 {
								 options.SingleLine = true;
							 });
			});
			logger = loggerFactory.CreateLogger<FaWindowManager>();
			fullscreenedProcessIds = new HashSet<int>();
			this.x = x;
			this.y = y;
			this.width = width;
			this.height = height;
			this.cpu = cpu;
		}
		internal void Run()
		{
			CheckFaProcessWindows();
		}
		internal Task Watch()
		{
			var timer = new Timer(TimeSpan.FromMilliseconds(100));
			timer.Elapsed += (object? sender, ElapsedEventArgs e) =>
			{
				timer.Stop();
				CheckFaProcessWindows();
				timer.Start();
			};
			timer.Start();
			logger.LogInformation(@"Watching for Forged Alliance processes...");
			return Task.Delay(-1);
		}
		private void CheckFaProcessWindows()
		{
			var faProcesses = Process.GetProcessesByName(@"ForgedAlliance");
			foreach (var faProcess in faProcesses)
			{
				var pid = faProcess.Id;
				if (fullscreenedProcessIds.Add(pid))
				{
					logger.LogInformation(@"[{ProcessId}] Detected {ProcessName} process", pid, faProcess.ProcessName);
					if (cpu && OperatingSystem.IsWindows())
					{
						faProcess.ProcessorAffinity = (int)Math.Pow(2, Environment.ProcessorCount) - 2;
						logger.LogInformation(@"[{ProcessId}] Set process affinity to {ProcessAffinity}", pid, Convert.ToString(faProcess.ProcessorAffinity, 2));
						faProcess.PriorityClass = ProcessPriorityClass.High;
						logger.LogInformation(@"[{ProcessId}] Set process priority to {ProcessPriority}", pid, faProcess.PriorityClass.ToString());
					}
					SpinWait.SpinUntil(() => faProcess.MainWindowHandle != 0); // Wait for the main window to exist before trying to manipulate it
					logger.LogInformation(@"[{ProcessId}] Detected main window", pid);
					SetBorderlessAndResize(pid, faProcess.MainWindowHandle);
					logger.LogInformation(@"[{ProcessId}] Window ready", pid);
					faProcess.Exited += (object? sender, EventArgs e) =>
					{
						fullscreenedProcessIds.Remove(pid);
						logger.LogInformation(@"[{ProcessId}] Process exited, freeing PID", pid);
					};
					faProcess.EnableRaisingEvents = true;
				}
			}
		}
		private void SetBorderlessAndResize(int pid, nint windowHandle)
		{
			ShowWindow(windowHandle, SW_RESTORE); // Can't resize window if it's minimized/maximized
			SetWindowLongW(windowHandle, GWL_STYLE, WS_BORDERLESS); // Remove frame from window style
			logger.LogInformation(@"[{ProcessId}] Made window borderless", pid);
			SetWindowPos(windowHandle, HWND_TOP, x, y, width, height, SWP_SHOWWINDOW | SWP_FRAMECHANGED); // Resize and apply window style
			logger.LogInformation(@"[{ProcessId}] Resized window", pid);
			ShowWindow(windowHandle, SW_SHOWMAXIMIZED); // Maximize window (seems to help drawing work correctly)
			ShowWindow(windowHandle, SW_RESTORE); // Restore to desired size after maximizing
			logger.LogInformation(@"[{ProcessId}] Redrew contents", pid);
		}
	}
}
