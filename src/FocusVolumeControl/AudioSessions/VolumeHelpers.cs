using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusVolumeControl.AudioSessions
{
	internal class VolumeHelpers
	{
		public static float GetAdjustedVolume(float startingVolume, int step, int ticks)
		{
			if(step <= 0)
			{
				step = 1;
			}

			var level = startingVolume;

			level += 0.01f * step * ticks;
			level = Math.Max(level, 0);
			level = Math.Min(level, 1);

			return level;
		}

		public static int GetVolumePercentage(float volume) => (int)Math.Round(volume * 100);

	}
}
