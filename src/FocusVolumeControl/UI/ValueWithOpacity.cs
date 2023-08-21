using Newtonsoft.Json;

namespace FocusVolumeControl.UI;

internal class ValueWithOpacity<T>
{
	[JsonProperty("value")]
	public required T Value { get; init; }

	[JsonProperty("opacity")]
	public required float Opacity { get; init; }

}
