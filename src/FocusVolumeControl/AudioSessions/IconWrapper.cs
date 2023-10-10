using BarRaider.SdTools;
using BitFaster.Caching.Lru;
using FocusVolumeControl.UI;
using System;
using System.Drawing;

namespace FocusVolumeControl.AudioSession
{
	public abstract class IconWrapper
	{
		protected static ConcurrentLru<string, string> _iconCache = new ConcurrentLru<string, string>(10);

		public abstract string GetIconData();

		internal const string FallbackIconData = "Images/encoderIcon";
	}

	internal class AppxIcon : IconWrapper
	{
		private readonly string _iconPath;

		public AppxIcon(string iconPath)
		{
			_iconPath = iconPath;
		}

		public override string GetIconData()
		{
			if(string.IsNullOrEmpty(_iconPath))
			{
				return FallbackIconData;
			}

			return _iconCache.GetOrAdd(_iconPath, (key) =>
			{
				var tmp = (Bitmap)Bitmap.FromFile(_iconPath);
				tmp.MakeTransparent();
				return Tools.ImageToBase64(tmp, true);
			});
		}

	}

	internal class NormalIcon : IconWrapper
	{
		private readonly string _iconPath;

		public NormalIcon(string iconPath)
		{
			_iconPath = iconPath;
		}

		public override string GetIconData()
		{
			if(string.IsNullOrEmpty(_iconPath))
			{
				return FallbackIconData;
			}

			return _iconCache.GetOrAdd(_iconPath, (key) =>
			{
				var tmp = IconExtraction.GetIcon(_iconPath);
				return Tools.ImageToBase64(tmp, true);
			});
		}
	}

	internal class RawIcon : IconWrapper
	{
		private readonly string _data;

		public RawIcon(string name, Func<Bitmap?> getIcon)
		{
			_data = _iconCache.GetOrAdd(name, (key) =>
			{
				var icon = getIcon();
				if (icon == null)
				{
					return FallbackIconData;
				}

				if (icon.Height < 48 && icon.Width < 48)
				{
					using var newImage = new Bitmap(48, 48);
					newImage.MakeTransparent();
					using var graphics = Graphics.FromImage(newImage);

					graphics.DrawImage(icon, 4, 4, 40, 40);


					return Tools.ImageToBase64(newImage, true);
				}
				else
				{
					return Tools.ImageToBase64(icon, true);
				}
			});
		}

		public override string GetIconData() => _data;
	}

}
