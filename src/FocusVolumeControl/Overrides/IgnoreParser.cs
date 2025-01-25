using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusVolumeControl.Overrides
{
	internal class IgnoreParser
	{
		public static List<string> Parse(string raw)
		{
			var ignores = new List<string>();

			if (raw == null)
			{
				return ignores;
			}

			var lines = raw.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				if (string.IsNullOrEmpty(line) || line.StartsWith("//"))
				{
					continue;
				}
				var str = line.Trim();
				ignores.Add(line.Trim());
			}

			return ignores;
		}

	}
}
