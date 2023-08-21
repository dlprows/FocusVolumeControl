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

	IAudioSession _currentAudioSession;
	AudioHelper _audioHelper = new AudioHelper();

	Thread _thread;
	Dispatcher _dispatcher;

	UIState _previousState;

	public DialAction(ISDConnection connection, InitialPayload payload) : base(connection, payload)
	{
		if (payload.Settings == null || payload.Settings.Count == 0)
		{
			settings = PluginSettings.CreateDefaultSettings();
			SaveSettings();
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


		_currentAudioSession = settings.FallbackBehavior == FallbackBehavior.SystemSounds ? _audioHelper.GetSystemSounds() : _audioHelper.GetSystemVolume();
	}

	public override async void DialDown(DialPayload payload)
	{
		//dial pressed down
		Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Down");
		await ToggleMuteAsync();
	}

	public override async void TouchPress(TouchpadPressPayload payload)
	{
		Logger.Instance.LogMessage(TracingLevel.INFO, "Touch Press");
		if (payload.IsLongPress)
		{
			_audioHelper.ResetAll();
		}
		else
		{
			await ToggleMuteAsync();
		}
	}

	async Task ToggleMuteAsync()
	{
		try
		{

			if (_currentAudioSession != null)
			{
				_currentAudioSession.ToggleMute();
				await UpdateStateIfNeeded();
			}
			else
			{
				await Connection.ShowAlert();
			}
		}
		catch (Exception ex)
		{
			await Connection.ShowAlert();
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unable to toggle mute: {ex.Message}");
		}
	}

	public override async void DialRotate(DialRotatePayload payload)
	{
		Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Rotate");
		//dial rotated. ticks positive for right, negative for left
		try
		{
			if (_currentAudioSession != null)
			{
				_currentAudioSession.IncrementVolumeLevel(1, payload.Ticks);
				await UpdateStateIfNeeded();
			}
			else
			{
				await Connection.ShowAlert();
			}
		}
		catch (Exception ex)
		{
			await Connection.ShowAlert();
			Logger.Instance.LogMessage(TracingLevel.ERROR, $"Unable to toggle mute: {ex.Message}");
		}
	}

	public override void DialUp(DialPayload payload)
	{
		//dial unpressed
		Logger.Instance.LogMessage(TracingLevel.INFO, "Dial Up");
	}

	public override void Dispose()
	{
		Logger.Instance.LogMessage(TracingLevel.DEBUG, "Disposing");
            if(_foregroundWindowChangedEvent != IntPtr.Zero)
            {
                Native.UnhookWinEvent(_foregroundWindowChangedEvent);
            }
		_dispatcher.InvokeShutdown();
	}

	public override async void OnTick()
	{
		//called once every 1000ms and can be used for updating the title/image of the key
		var activeSession = _audioHelper.GetActiveSession(settings.FallbackBehavior);

		if(activeSession != null)
		{
			_currentAudioSession = activeSession;
		}

		await UpdateStateIfNeeded();
	}

	private async Task UpdateStateIfNeeded()
	{
		if (_currentAudioSession != null)
		{

			var uiState = new UIState(_currentAudioSession);

			if ( _previousState != null && uiState != null &&
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


	public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
	{
	}


	public override void ReceivedSettings(ReceivedSettingsPayload payload)
	{
		Tools.AutoPopulateSettings(settings, payload.Settings);
		SaveSettings();
	}

	private Task SaveSettings()
	{
		return Connection.SetSettingsAsync(JObject.FromObject(settings));
	}


        public void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
		OnTick();
        }


}
