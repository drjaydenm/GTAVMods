using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAVScript
{
	public static class Logging
	{
		private static bool setup = false;

		public static void SetupLogging()
		{
			if (!setup)
			{
				var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
				var logPath = Path.Combine(desktop, "log.txt");

				Log.Logger = new LoggerConfiguration()
					.WriteTo.File(logPath)
					.CreateLogger();

				setup = true;
			}
		}
	}
}
