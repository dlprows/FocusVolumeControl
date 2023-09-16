using BarRaider.SdTools;
using BarRaider.SdTools.Payloads;
using FocusVolumeControl.AudioSessions;
using FocusVolumeControl.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FocusVolumeControl;

[PluginActionId("com.dlprows.focusvolumecontrol.dialaction")]
public class DialAction : EncoderBase
{
	private class PluginSettings
	{
		[JsonProperty("fallbackBehavior")]
		public FallbackBehavior FallbackBehavior { get; set; }

		[JsonProperty("stepSize")]
		public int StepSize { get; set; }

		public static PluginSettings CreateDefaultSettings()
		{
			PluginSettings instance = new PluginSettings();
			instance.FallbackBehavior = FallbackBehavior.SystemSounds;
			return instance;
		}
	}

	private PluginSettings settings;

	IntPtr _foregroundWindowChangedEvent;
	Native.WinEventDelegate _delegate;

	AudioHelper _audioHelper = new AudioHelper();

	Thread _thread;
	Dispatcher _dispatcher;

	UIState _previousState;

	public DialAction(ISDConnection connection, InitialPayload payload) : base(connection, payload)
	{
		if (payload.Settings == null || payload.Settings.Count == 0)
		{
			settings = PluginSettings.CreateDefaultSettings();
			_ = SaveSettings();
		}
		else
		{
			settings = payload.Settings.ToObject<PluginSettings>();
		}

		_thread = new Thread(() =>
		{
			Logger.Instance.LogMessage(TracingLevel.DEBUG, "Registering for events");
			_delegate = new Native.WinEventDelegate(WinEventProc);
			_foregroundWindowChangedEvent = Native.RegisterForForegroundWindowChangedEvent(_delegate);

			Logger.Instance.LogMessage(TracingLevel.DEBUG, "Starting Dispatcher");
			_dispatcher = Dispatcher.CurrentDispatcher;
			Dispatcher.Run();
			Logger.Instance.LogMessage(TracingLevel.DEBUG, "Dispatcher Stopped");
		});
		_thread.SetApartmentState(ApartmentState.STA);
		_thread.Start();

		var session = _audioHelper.GetActiveSession(settings.FallbackBehavior);
		_ = UpdateStateIfNeeded(session);
	}

	public override void Dispose()
	{
		Logger.Instance.LogMessage(TracingLevel.DEBUG, "Disposing");
		if (_foregroundWindowChangedEvent != IntPtr.Zero)
		{
			Native.UnhookWinEvent(_foregroundWindowChangedEvent);
		}
		_dispatcher.InvokeShutdown();
	}

	public override async void DialDown(DialPayload payload)
	{
		try
		{
			Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Down");
			await ToggleMuteAsync();
		}
		catch (Exception ex)
		{
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unexpected Error in DialDown:\n {ex}");
		}
	}
	public override void DialUp(DialPayload payload) { }

	public override async void TouchPress(TouchpadPressPayload payload)
	{
		try
		{
			Logger.Instance.LogMessage(TracingLevel.INFO, "Touch Press");
			if (payload.IsLongPress)
			{
				await ResetAllAsync();
			}
			else
			{
				await ToggleMuteAsync();
			}
		}
		catch (Exception ex)
		{
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unexpected Error in TouchPress:\n {ex}");
		}
	}

	public override async void DialRotate(DialRotatePayload payload)
	{
		try
		{
			Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Rotate");
			//dial rotated. ticks positive for right, negative for left
			var activeSession = _audioHelper.Current;
			if (activeSession != null)
			{
				activeSession.IncrementVolumeLevel(settings.StepSize, payload.Ticks);
				await UpdateStateIfNeeded(activeSession);
			}
			else
			{
				await Connection.ShowAlert();
			}
		}
		catch (Exception ex)
		{
			_audioHelper.ResetCache();
			await Connection.ShowAlert();
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unable to increment volume:\n {ex}");
		}
	}

	async Task ResetAllAsync()
	{
		try
		{
			_audioHelper.ResetAll();
		}
		catch
		{
			_audioHelper.ResetCache();
			await Connection.ShowAlert();
			throw;
		}
	}

	async Task ToggleMuteAsync()
	{
		try
		{
			var activeSession = _audioHelper.Current;
			if (activeSession != null)
			{
				activeSession.ToggleMute();
				await UpdateStateIfNeeded(activeSession);
			}
			else
			{
				await Connection.ShowAlert();
			}
		}
		catch
		{
			_audioHelper.ResetCache();
			await Connection.ShowAlert();
			throw;
		}
	}

	public override async void OnTick()
	{
		try
		{
			//called once every 1000ms and can be used for updating the title/image of the key
			var activeSession = _audioHelper.GetActiveSession(settings.FallbackBehavior);

			await UpdateStateIfNeeded(activeSession);
		}
		catch (Exception ex)
		{
			_audioHelper.ResetCache();
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception on Tick:\n {ex}");
		}
	}

	private async Task UpdateStateIfNeeded(IAudioSession audioSession)
	{
		try
		{
			if (audioSession != null)
			{

				var uiState = new UIState(audioSession);

				if (_previousState != null && uiState != null &&
					uiState.Title == _previousState.Title &&
					uiState.Value.Value == _previousState.Value.Value &&
					uiState.Value.Opacity == _previousState.Value.Opacity &&
					uiState.Indicator.Value == _previousState.Indicator.Value &&
					uiState.Indicator.Opacity == _previousState.Indicator.Opacity &&
					uiState.icon.Value == _previousState.icon.Value &&
					uiState.icon.Opacity == _previousState.icon.Opacity
					)
				{
					return;
				}

				await Connection.SetFeedbackAsync(uiState);
				_previousState = uiState;
			}
		}
		catch (Exception ex)
		{
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Failed to update screen\n {ex}");
		}
	}


	public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
	{
	}


	public override void ReceivedSettings(ReceivedSettingsPayload payload)
	{
		try
		{
			Tools.AutoPopulateSettings(settings, payload.Settings);
			_ = SaveSettings();
		}
		catch (Exception ex)
		{
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unexpected Error in SaveSettings:\n {ex}");
		}
	}

	private async Task SaveSettings()
	{
		try
		{
			await Connection.SetSettingsAsync(JObject.FromObject(settings));
		}
		catch (Exception ex)
		{
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unexpected Error in SaveSettings:\n {ex}");
		}
	}


	public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
	{
		try
		{
			OnTick();
		}
		catch (Exception ex)
		{
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unexpected Error in DialDown:\n {ex}");
		}
	}
}
