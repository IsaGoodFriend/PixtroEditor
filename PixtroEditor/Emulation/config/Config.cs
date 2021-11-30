using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Pixtro.Emulation
{
	public class Config
	{
		public static string ControlDefaultPath => Path.Combine("", "defctrl.json");

		/// <remarks>
		/// <c>AppliesTo[0]</c> is used as the group label, and
		/// <c>Config.PreferredCores[AppliesTo[0]]</c> (lookup on global <see cref="Config"/> instance) determines the currently selected option.
		/// The tuples' order determines the order of menu items.
		/// </remarks>
		
		public static string DefaultIniPath { get; private set; } = Path.Combine("", "config.ini");

		// Shenanigans
		public static void SetDefaultIniPath(string newDefaultIniPath)
		{
			DefaultIniPath = newDefaultIniPath;
		}

		public Config()
		{
			if (AllTrollers.Count == 0
				&& AllTrollersAutoFire.Count == 0
				&& AllTrollersAnalog.Count == 0
				&& AllTrollersFeedbacks.Count == 0)
			{
				var cd = ConfigService.Load<DefaultControls>(ControlDefaultPath);
				AllTrollers = cd.AllTrollers;
				AllTrollersAutoFire = cd.AllTrollersAutoFire;
				AllTrollersAnalog = cd.AllTrollersAnalog;
				AllTrollersFeedbacks = cd.AllTrollersFeedbacks;
			}
		}

		public void ResolveDefaults()
		{
			PathEntries.ResolveWithDefaults();
			HotkeyBindings.ResolveWithDefaults();
			PathEntries.RefreshTempPath();
			if (string.IsNullOrEmpty(TemplatesFolder) || !Directory.Exists(TemplatesFolder))
				TemplatesFolder = Path.Combine(Directory.GetCurrentDirectory(), "templates");
		}

		/// <summary>
		/// Used to determine the system a rom is classified as (and thus which core to use) for the times when our romloading autodetect magic can't determine a system.
		/// Keys are file extensions, include the leading period in each, and use lowercase;
		/// values are system IDs, use <see cref="string.Empty"/> for unset (though <see langword="null"/> should also work, omitting will remove from UI).
		/// </summary>
		public Dictionary<string, string> PreferredPlatformsForExtensions { get; set; } = new Dictionary<string, string>
		{
			[".bin"] = "",
			[".cue"] = "",
			[".img"] = "",
			[".iso"] = "",
			[".rom"] = "",
		};

		public PathEntryCollection PathEntries { get; set; } = new PathEntryCollection();

		// BIOS Paths
		// key: sysId+firmwareId; value: absolute path
		public Dictionary<string, string> FirmwareUserSpecifications { get; set; } = new Dictionary<string, string>();

		// General Client Settings
		public int InputHotkeyOverrideOptions { get; set; }
		public bool NoMixedInputHokeyOverride { get; set; }

		public bool StackOSDMessages { get; set; } = true;

		public ZoomFactors TargetZoomFactors { get; set; } = new ZoomFactors();

		public string TemplatesFolder { get; set; } = "";

		// choose between 0 and 256
		public int TargetScanlineFilterIntensity { get; set; } = 128;
		public int TargetDisplayFilter { get; set; }
		public int DispFinalFilter { get; set; } = 0; // None
		public string DispUserFilterPath { get; set; } = "";
		public bool PauseWhenMenuActivated { get; set; } = true;
		public bool SaveWindowPosition { get; set; } = true;
		public bool StartPaused { get; set; }
		public bool StartFullscreen { get; set; }
		public int MainWndx { get; set; } = -1; // Negative numbers will be ignored
		public int MainWndy { get; set; } = -1;
		public int MainWndW { get; set; } = -1; // Negative numbers will be ignored
		public int MainWndH { get; set; } = -1;
		public bool MainWndMax { get; set; }
		public bool RunInBackground { get; set; } = true;
		public bool AcceptBackgroundInput { get; set; }
		public bool AcceptBackgroundInputControllerOnly { get; set; }
		public bool HandleAlternateKeyboardLayouts { get; set; }
		public bool SingleInstanceMode { get; set; }
		public bool AllowUdlr { get; set; }
		public bool ShowContextMenu { get; set; } = true;
		public bool HotkeyConfigAutoTab { get; set; } = true;
		public bool InputConfigAutoTab { get; set; } = true;
		public bool SkipWaterboxIntegrityChecks { get; set; } = false;
		public int AutofireOn { get; set; } = 1;
		public int AutofireOff { get; set; } = 1;
		public bool AutofireLagFrames { get; set; } = true;
		public int SaveSlot { get; set; } // currently selected savestate slot
		public bool AutoLoadLastSaveSlot { get; set; }
		public bool SkipLagFrame { get; set; }
		public bool SuppressAskSave { get; set; }
		public bool AviCaptureOsd { get; set; }
		public bool ScreenshotCaptureOsd { get; set; }
		public bool FirstBoot { get; set; } = true;
		public bool UpdateAutoCheckEnabled { get; set; }
		public DateTime? UpdateLastCheckTimeUtc { get; set; }
		public string UpdateLatestVersion { get; set; } = "";
		public string UpdateIgnoreVersion { get; set; } = "";
		public bool SkipOutdatedOsCheck { get; set; }

		/// <summary>
		/// Makes a .bak file before any saveram-writing operation (could be extended to make timestamped backups)
		/// </summary>
		public bool BackupSaveram { get; set; } = true;

		/// <summary>
		/// Whether to make AutoSave files at periodic intervals
		/// </summary>
		public bool AutosaveSaveRAM { get; set; }

		/// <summary>
		/// Intervals at which to make AutoSave files
		/// </summary>
		public int FlushSaveRamFrames { get; set; }

		public bool TurboSeek { get; set; }

		public ClientProfile SelectedProfile { get; set; } = ClientProfile.Unknown;

		// N64
		public bool N64UseCircularAnalogConstraint { get; set; } = true;

		// Run-Control settings
		public int FrameProgressDelayMs { get; set; } = 500; // how long until a frame advance hold turns into a frame progress?
		public int FrameSkip { get; set; } = 4;
		public int SpeedPercent { get; set; } = 100;
		public int SpeedPercentAlternate { get; set; } = 400;
		public bool ClockThrottle { get; set; }= true;
		public bool AutoMinimizeSkipping { get; set; } = true;
		public bool VSyncThrottle { get; set; } = false;

		public RewindConfig Rewind { get; set; } = new RewindConfig();

		public SaveStateConfig Savestates { get; set; } = new SaveStateConfig();

		/// <summary>
		/// Use vsync when presenting all 3d accelerated windows.
		/// For the main window, if VSyncThrottle = false, this will try to use vsync without throttling to it
		/// </summary>
		public bool VSync { get; set; }

		/// <summary>
		/// Tries to use an alternate vsync mechanism, for video cards that just can't do it right
		/// </summary>
		public bool DispAlternateVsync { get; set; }

		// Display options
		public bool DisplayFps { get; set; }
		public bool DisplayFrameCounter { get; set; }
		public bool DisplayLagCounter { get; set; }
		public bool DisplayInput { get; set; }
		public bool DisplayRerecordCount { get; set; }
		public bool DisplayMessages { get; set; } = true;

		public bool DispFixAspectRatio { get; set; } = true;
		public bool DispFixScaleInteger { get; set; }
		public bool DispFullscreenHacks { get; set; }
		public bool DispAutoPrescale { get; set; }
		public int DispSpeedupFeatures { get; set; } = 2;

		public MessagePosition Fps { get; set; } = DefaultMessagePositions.Fps.Clone();
		public MessagePosition FrameCounter { get; set; } = DefaultMessagePositions.FrameCounter.Clone();
		public MessagePosition LagCounter { get; set; } = DefaultMessagePositions.LagCounter.Clone();
		public MessagePosition InputDisplay { get; set; } = DefaultMessagePositions.InputDisplay.Clone();
		public MessagePosition ReRecordCounter { get; set; } = DefaultMessagePositions.ReRecordCounter.Clone();
		public MessagePosition Messages { get; set; } = DefaultMessagePositions.Messages.Clone();
		public MessagePosition Autohold { get; set; } = DefaultMessagePositions.Autohold.Clone();
		public MessagePosition RamWatches { get; set; } = DefaultMessagePositions.RamWatches.Clone();

		public int MessagesColor { get; set; } = DefaultMessagePositions.MessagesColor;
		public int AlertMessageColor { get; set; } = DefaultMessagePositions.AlertMessageColor;
		public int LastInputColor { get; set; } = DefaultMessagePositions.LastInputColor;
		public int MovieInput { get; set; } = DefaultMessagePositions.MovieInput;

		public int DispPrescale { get; set; } = 1;

		private static bool DetectDirectX()
		{
			if (OSTailoredCode.IsUnixHost) return false;
			var p = OSTailoredCode.LinkedLibManager.LoadOrZero("d3dx9_43.dll");
			if (p == IntPtr.Zero) return false;
			OSTailoredCode.LinkedLibManager.FreeByPtr(p);
			return true;
		}

		public EDispMethod DispMethod { get; set; } = DetectDirectX() ? EDispMethod.SlimDX9 : EDispMethod.OpenGL;

		public int DispChromeFrameWindowed { get; set; } = 2;
		public bool DispChromeStatusBarWindowed { get; set; } = true;
		public bool DispChromeCaptionWindowed { get; set; } = true;
		public bool DispChromeMenuWindowed { get; set; } = true;
		public bool DispChromeStatusBarFullscreen { get; set; }
		public bool DispChromeMenuFullscreen { get; set; }
		public bool DispChromeFullscreenAutohideMouse { get; set; } = true;
		public bool DispChromeAllowDoubleClickFullscreen { get; set; } = true;

		public EDispManagerAR DispManagerAR { get; set; } = EDispManagerAR.System;

		// these are misnomers. they're actually a fixed size (fixme on major release)
		public int DispCustomUserARWidth { get; set; } = -1;
		public int DispCustomUserARHeight { get; set; } = -1;

		// these are more like the actual AR ratio (i.e. 4:3) (fixme on major release)
		public float DispCustomUserArx { get; set; } = -1;
		public float DispCustomUserAry { get; set; } = -1;

		//these default to 0 because by default we crop nothing
		public int DispCropLeft { get; set; } = 0;
		public int DispCropTop { get; set; } = 0;
		public int DispCropRight { get; set; } = 0;
		public int DispCropBottom { get; set; } = 0;

		// Sound options
		public ESoundOutputMethod SoundOutputMethod { get; set; } = DetectDirectX() ? ESoundOutputMethod.DirectSound : ESoundOutputMethod.OpenAL;
		public bool SoundEnabled { get; set; } = true;
		public bool SoundEnabledNormal { get; set; } = true;
		public bool SoundEnabledRWFF { get; set; } = true;
		public bool MuteFrameAdvance { get; set; } = true;
		public int SoundVolume { get; set; } = 100; // Range 0-100
		public int SoundVolumeRWFF { get; set; } = 50; // Range 0-100
		public bool SoundThrottle { get; set; }
		public string SoundDevice { get; set; } = "";
		public int SoundBufferSizeMs { get; set; } = 100;

		// Emulation core settings
		internal Dictionary<string, JToken> CoreSettings { get; set; } = new Dictionary<string, JToken>();
		internal Dictionary<string, JToken> CoreSyncSettings { get; set; } = new Dictionary<string, JToken>();

		public Dictionary<string, ToolDialogSettings> CommonToolSettings { get; set; } = new Dictionary<string, ToolDialogSettings>();
		public Dictionary<string, Dictionary<string, object>> CustomToolSettings { get; set; } = new Dictionary<string, Dictionary<string, object>>();

		public CheatConfig Cheats { get; set; } = new CheatConfig();

		// Play Movie Dialog
		public bool PlayMovieIncludeSubDir { get; set; }
		public bool PlayMovieMatchHash { get; set; } = true;


		public BindingCollection HotkeyBindings { get; set; } = new BindingCollection();

		// Analog Hotkey values
		public int AnalogLargeChange { get; set; } = 10;
		public int AnalogSmallChange { get; set; } = 1;

		// [ControllerType][ButtonName] => Physical Bind
		public Dictionary<string, Dictionary<string, string>> AllTrollers { get; set; } = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire { get; set; } = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, AnalogBind>> AllTrollersAnalog { get; set; } = new Dictionary<string, Dictionary<string, AnalogBind>>();
		public Dictionary<string, Dictionary<string, FeedbackBind>> AllTrollersFeedbacks { get; set; } = new Dictionary<string, Dictionary<string, FeedbackBind>>();

		/// <remarks>as this setting spans multiple cores and doesn't actually affect the behavior of any core, it hasn't been absorbed into the new system</remarks>
		public bool GbAsSgb { get; set; }
		public string LibretroCore { get; set; }

		public bool DontTryOtherCores { get; set; }

		// ReSharper disable once UnusedMember.Global
		public string LastWrittenFrom { get; set; } = VersionInfo.MainVersion;

		// ReSharper disable once UnusedMember.Global
		public string LastWrittenFromDetailed { get; set; } = VersionInfo.GetEmuVersion();

		public EHostInputMethod HostInputMethod { get; set; } = OSTailoredCode.IsUnixHost ? EHostInputMethod.OpenTK : EHostInputMethod.DirectInput;

		public bool UseStaticWindowTitles { get; set; }
	}
}
