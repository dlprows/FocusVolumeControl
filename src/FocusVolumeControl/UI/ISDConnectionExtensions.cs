using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace FocusVolumeControl.UI;

internal static class ISDConnectionExtensions
{
	public static async Task SetFeedbackAsync(this ISDConnection _this, object feedbackPayload)
	{
		await _this.SetFeedbackAsync(JObject.FromObject(feedbackPayload));
	}
}
