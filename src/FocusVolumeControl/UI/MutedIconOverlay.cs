using BarRaider.SdTools;
using BitFaster.Caching.Lru;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace FocusVolumeControl.UI;

// Draws a red diagonal slash over the app icon when muted,
// matching the look of Elgato's Volume Controller plugin.
internal static class MutedIconOverlay
{
	static readonly ConcurrentLru<string, string> _cache = new ConcurrentLru<string, string>(20);

	public static string Apply(string iconData)
	{
		if (string.IsNullOrEmpty(iconData))
		{
			return iconData;
		}

		try
		{
			return _cache.GetOrAdd(iconData, Build);
		}
		catch
		{
			return iconData;
		}
	}

	static string Build(string iconData)
	{
		using var src = Load(iconData);
		if (src == null)
		{
			return iconData;
		}

		var size = Math.Max(src.Width, src.Height);
		using var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
		using (var g = Graphics.FromImage(bmp))
		{
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.InterpolationMode = InterpolationMode.HighQualityBicubic;
			g.PixelOffsetMode = PixelOffsetMode.HighQuality;
			g.DrawImage(src, (size - src.Width) / 2, (size - src.Height) / 2, src.Width, src.Height);

			// Same look as Elgato's Volume Controller: thin red slash, corner to corner
			var width = size * 0.07f;
			var margin = size * 0.04f;
			using var red = new Pen(Color.FromArgb(255, 255, 58, 68), width) { StartCap = LineCap.Flat, EndCap = LineCap.Flat };
			g.DrawLine(red, margin, margin, size - margin, size - margin);
		}

		return Tools.ImageToBase64(bmp, true);
	}

	static Bitmap Load(string iconData)
	{
		const string marker = "base64,";
		var idx = iconData.IndexOf(marker, StringComparison.Ordinal);
		if (idx >= 0)
		{
			var bytes = Convert.FromBase64String(iconData.Substring(idx + marker.Length));
			using var ms = new MemoryStream(bytes);
			using var img = Image.FromStream(ms);
			return new Bitmap(img);
		}

		// Icon given as a path relative to the plugin (e.g. "Images/encoderIcon")
		var dir = AppDomain.CurrentDomain.BaseDirectory;
		foreach (var candidate in new[] { iconData + "@2x.png", iconData + ".png", iconData })
		{
			var path = Path.Combine(dir, candidate);
			if (File.Exists(path))
			{
				using var img = Image.FromFile(path);
				return new Bitmap(img);
			}
		}

		return null;
	}
}
