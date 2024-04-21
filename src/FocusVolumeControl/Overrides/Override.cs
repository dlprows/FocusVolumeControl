using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FocusVolumeControl.Overrides
{
	public class Override
	{
		public MatchType MatchType { get; set; }
		public string WindowQuery { get; set; }
		public string AudioProcessName { get; set; }
	}
}
