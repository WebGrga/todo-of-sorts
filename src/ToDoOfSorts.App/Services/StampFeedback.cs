using ToDoOfSorts.Core;

namespace ToDoOfSorts.App.Services;

public sealed class StampFeedback : IFeedbackService
{
#if ANDROID
    private global::Android.Media.SoundPool? _pool;
    private int _stamp, _win;
    private readonly HashSet<int> _loaded = [];
#elif IOS
    private AVFoundation.AVAudioPlayer? _stamp, _win;
#endif
    private bool _prepared;
    public bool SystemReducedMotion
    {
        get
        {
#if IOS
            return UIKit.UIAccessibility.IsReduceMotionEnabled;
#elif ANDROID
            try { return global::Android.Provider.Settings.Global.GetFloat(global::Android.App.Application.Context.ContentResolver,
                global::Android.Provider.Settings.Global.AnimatorDurationScale, 1) == 0; } catch { return false; }
#else
            return false;
#endif
        }
    }
    public async Task PrepareAsync()
    {
        if (_prepared) return;
        async Task<string> Copy(string name)
        {
            var path = Path.Combine(FileSystem.CacheDirectory, name);
            if (!File.Exists(path)) { await using var input = await FileSystem.OpenAppPackageFileAsync(name); await using var output = File.Create(path); await input.CopyToAsync(output); }
            return path;
        }
        var stamp = await Copy("stamp.wav"); var win = await Copy("day-win.wav");
#if ANDROID
        using var attributes = new global::Android.Media.AudioAttributes.Builder().SetUsage(global::Android.Media.AudioUsageKind.AssistanceSonification)!.SetContentType(global::Android.Media.AudioContentType.Sonification)!.Build();
        _pool = new global::Android.Media.SoundPool.Builder().SetMaxStreams(3)!.SetAudioAttributes(attributes)!.Build();
        _pool!.LoadComplete += (_, e) => { if (e.Status == 0) _loaded.Add(e.SampleId); };
        _stamp = _pool.Load(stamp, 1); _win = _pool.Load(win, 1);
#elif IOS
        AVFoundation.AVAudioSession.SharedInstance().SetCategory(AVFoundation.AVAudioSessionCategory.Ambient);
        _stamp = AVFoundation.AVAudioPlayer.FromUrl(Foundation.NSUrl.FromFilename(stamp)); _stamp?.PrepareToPlay();
        _win = AVFoundation.AVAudioPlayer.FromUrl(Foundation.NSUrl.FromFilename(win)); _win?.PrepareToPlay();
#endif
        _prepared = true;
    }
    public void Impact(AppSettings settings, bool dayWin = false)
    {
        if (settings.Haptics)
        {
            try
            {
#if IOS
                var style = settings.Theme == Experience.Calm ? UIKit.UIImpactFeedbackStyle.Light : UIKit.UIImpactFeedbackStyle.Heavy;
                var nativeView = Platform.GetCurrentUIViewController()?.View;
                UIKit.UIImpactFeedbackGenerator? generator = null;
                if (OperatingSystem.IsIOSVersionAtLeast(17, 5))
                { if (nativeView is not null) generator = UIKit.UIImpactFeedbackGenerator.GetFeedbackGenerator(style, nativeView); }
                else generator = CreateLegacyImpact(style);
                using (generator) { generator?.Prepare(); generator?.ImpactOccurred(); }
                if (dayWin) { using var success = new UIKit.UINotificationFeedbackGenerator(); success.NotificationOccurred(UIKit.UINotificationFeedbackType.Success); }
#elif ANDROID
                var context = global::Android.App.Application.Context;
                var vibrator = OperatingSystem.IsAndroidVersionAtLeast(31)
                    ? ((global::Android.OS.VibratorManager?)context.GetSystemService(global::Android.Content.Context.VibratorManagerService))?.DefaultVibrator
                    : (global::Android.OS.Vibrator?)context.GetSystemService(global::Android.Content.Context.VibratorService);
                if (OperatingSystem.IsAndroidVersionAtLeast(26)) vibrator?.Vibrate(global::Android.OS.VibrationEffect.CreateOneShot(settings.Theme == Experience.Calm ? 15 : dayWin ? 65 : 35, settings.Theme == Experience.Calm ? 70 : 200));
                else HapticFeedback.Default.Perform(HapticFeedbackType.Click);
#endif
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
        }
        if (!settings.Sound) return;
#if ANDROID
        var manager = (global::Android.Media.AudioManager?)global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.AudioService);
        if (manager?.RingerMode != global::Android.Media.RingerMode.Normal) return;
        var sample = dayWin ? _win : _stamp;
        var volume = settings.Theme == Experience.Calm ? .25f : .75f;
        if (_loaded.Contains(sample)) _pool?.Play(sample, volume, volume, 1, 0, 1);
#elif IOS
        var player = dayWin ? _win : _stamp;
        if (player is not null) { player.Volume = settings.Theme == Experience.Calm ? .25f : .75f; player.CurrentTime = 0; player.Play(); }
#endif
    }
#if IOS
    [System.Runtime.Versioning.ObsoletedOSPlatform("ios17.5")]
    private static UIKit.UIImpactFeedbackGenerator CreateLegacyImpact(UIKit.UIImpactFeedbackStyle style) => new(style);
#endif
}
