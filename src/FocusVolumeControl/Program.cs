using BarRaider.SdTools;

namespace FocusVolumeControl;

internal class Program
{
	static void Main(string[] args)
	{
#if DEBUG
		// Uncomment this line of code to allow for debugging
		//while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }
#endif

		SDWrapper.Run(args);
	}
}
