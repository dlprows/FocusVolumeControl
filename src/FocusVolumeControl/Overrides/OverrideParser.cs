using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusVolumeControl.Overrides
{
	internal class OverrideParser
	{
		public static List<Override> Parse(string raw)
		{
			var overrides = new List<Override>();

			if (raw == null)
			{
				return overrides;
			}

			var lines = raw.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);


			Override tmp = null;

			foreach (var line in lines)
			{
				if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
				{
					continue;
				}

				var split = line.Split(':');
				if (split.Length > 1)
				{
					if (!string.IsNullOrEmpty(tmp?.WindowQuery) && !string.IsNullOrEmpty(tmp?.AudioProcessName))
					{
						overrides.Add(tmp);
					}
					tmp = new Override();

					if (string.Equals(split[0], "eq", StringComparison.OrdinalIgnoreCase))
					{
						tmp.MatchType = MatchType.Equal;
					}
					else if (string.Equals(split[0], "start", StringComparison.OrdinalIgnoreCase))
					{
						tmp.MatchType = MatchType.StartsWith;
					}
					else if (string.Equals(split[0], "end", StringComparison.OrdinalIgnoreCase))
					{
						tmp.MatchType = MatchType.EndsWith;
					}
					else if (string.Equals(split[0], "regex", StringComparison.OrdinalIgnoreCase))
					{
						tmp.MatchType = MatchType.Regex;
					}
					else
					{
						continue;
					}
					tmp.WindowQuery = split[1].Trim();
				}
				else if (tmp != null)
				{
					tmp.AudioProcessName = split[0].Trim();
				}
			}

			if (!string.IsNullOrEmpty(tmp?.WindowQuery) && !string.IsNullOrEmpty(tmp?.AudioProcessName))
			{
				overrides.Add(tmp);
			}


			return overrides;
		}
	}
}
