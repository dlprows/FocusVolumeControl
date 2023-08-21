using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BarRaider.SdTools;
using Newtonsoft.Json;
using FocusVolumeControl.AudioSessions;

namespace FocusVolumeControl.UI;

internal class UIState
{
	[JsonProperty("title")]
	public string Title { get; private init; }

	[JsonProperty("value")]
	public ValueWithOpacity<string> Value { get; private init; }

	[JsonProperty("indicator")]
	public ValueWithOpacity<float>Indicator { get; private init; }

	[JsonProperty("icon")]
	public ValueWithOpacity<string> icon { get; private init; }

	public UIState(IAudioSession session)
	{
		var volume = session.GetVolumeLevel();
		var opacity = session.IsMuted() ? 0.5f : 1;
		var iconData = session.GetIcon();

		Title = session.DisplayName;
		Value = new() { Value = $"{volume}%", Opacity = opacity };
		Indicator = new() { Value = volume, Opacity = opacity };
		icon = new() { Value = iconData, Opacity = opacity };
	}

}
