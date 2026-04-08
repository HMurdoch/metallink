using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace MetalLink.Desktop.Views;

public partial class IntroWindow : Window
{
    private Avalonia.PixelRect? _targetBounds;

    public void SetTargetBounds(Avalonia.PixelRect bounds)
    {
        _targetBounds = bounds;
    }

    private Views.Controls.FfmpegLoopingVideoView? _video;
    private Process? _ffplay;
    private CancellationTokenSource? _playCts;

    public IntroWindow()
    {
        InitializeComponent();
        _video = this.FindControl<Views.Controls.FfmpegLoopingVideoView>("IntroVideo");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    public async Task PlayAsync(bool playVideo = true)
    {
        if (_video is null)
        {
            Console.WriteLine("[WARN] Intro: _video control not found.");
            return;
        }

        _playCts = new CancellationTokenSource();
        var ct = _playCts.Token;

        var src = new Uri("avares://MetalLink.Desktop/Assets/intro_video_6sec.mp4");

        try
        {
            Console.WriteLine("[INFO] Intro: Ensuring FFmpeg is ready...");
            await EnsureFfmpegReadyAsync();
            if (ct.IsCancellationRequested) return;

            var inputPath = ResolveInputPath(src);
            var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
            if (ct.IsCancellationRequested) return;

            var stream = mediaInfo.VideoStreams.FirstOrDefault();
            if (stream is not null)
            {
                var maxH = 900.0;
                var h = Math.Min(maxH, stream.Height);
                var w = h * stream.Width / stream.Height;
                Width = w;
                Height = h;

                var bounds = _targetBounds ?? (Screens.Primary is not null
                    ? (Screens.ScreenFromWindow(this) ?? Screens.Primary).WorkingArea
                    : default);

                if (bounds.Width > 0 && bounds.Height > 0)
                {
                    var x = bounds.X + (bounds.Width - (int)Width) / 2;
                    var y = bounds.Y + (bounds.Height - (int)Height) / 2;
                    Position = new PixelPoint(x, y);
                }
            }

            if (!playVideo)
            {
                Console.WriteLine("[DEBUG] Intro: playVideo is false. Showing last frame.");
                var lastFramePath = Path.Combine(Path.GetTempPath(), "metallink_intro_last_frame.jpg");
                var ffmpegExe = Path.Combine(FFmpeg.ExecutablesPath, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
                var args = $"-y -sseof -1 -i \"{inputPath}\" -update 1 -frames:v 1 \"{lastFramePath}\"";
                var psi = new ProcessStartInfo { FileName = ffmpegExe, Arguments = args, CreateNoWindow = true, UseShellExecute = false };
                using var p = Process.Start(psi);
                if (p != null) await p.WaitForExitAsync(ct);

                if (File.Exists(lastFramePath) && !ct.IsCancellationRequested)
                {
                    _video.Source = new Uri(lastFramePath);
                    _video.Opacity = 1;
                    await Task.Delay(1000, ct);
                }
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Intro setup error: {ex.Message}");
        }

        // Play sequence
        var tcs = new TaskCompletionSource<bool>();
        void Handler(object? s, EventArgs e) {
            Console.WriteLine("[DEBUG] Intro: FirstFrameRendered received.");
            tcs.TrySetResult(true);
        }

        _video.FirstFrameRendered += Handler;

        try
        {
            _video.Source = src;
            _video.Opacity = 0;

            if (_video.IsReady)
            {
                Console.WriteLine("[DEBUG] Intro: Video is already ready (cached).");
                tcs.TrySetResult(true);
            }

            Console.WriteLine("[DEBUG] Intro: Waiting for first frame...");
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(10000, ct));
            if (completed != tcs.Task && !ct.IsCancellationRequested)
            {
                Console.WriteLine("[WARN] Intro: First frame wait timed out (10s).");
            }

            if (ct.IsCancellationRequested) return;

            try
            {
                var inputPath = ResolveInputPath(src);
                _ffplay = await StartAudioAsync(inputPath);
            }
            catch (Exception ex) { Console.WriteLine($"[WARN] Intro audio failed: {ex.Message}"); }

            Console.WriteLine("[DEBUG] Intro: Fade-in.");
            await FadeOpacityAsync(_video, 0, 1, 1000, ct);
            
            if (!ct.IsCancellationRequested)
            {
                Console.WriteLine("[DEBUG] Intro: Playing for 4s.");
                await Task.Delay(4000, ct);
            }

            if (!ct.IsCancellationRequested)
            {
                Console.WriteLine("[DEBUG] Intro: Fade-out.");
                await FadeOpacityAsync(_video, 1, 0, 1000, ct);
            }
        }
        finally
        {
            _video.FirstFrameRendered -= Handler;
            try { if (_ffplay is { HasExited: false }) _ffplay.Kill(true); } catch { }
            Console.WriteLine("[DEBUG] Intro: PlayAsync finished.");
        }
    }

    private string ResolveInputPath(Uri uri)
    {
        if (uri.Scheme == "avares")
        {
            var assets = AssetLoader.Open(uri);
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetFileName(uri.LocalPath));
            using (var fileStream = File.Create(tempPath))
            {
                assets.CopyTo(fileStream);
            }
            return tempPath;
        }
        return uri.LocalPath;
    }

    private async Task<Process?> StartAudioAsync(string videoPath)
    {
        var ffplayExe = Path.Combine(FFmpeg.ExecutablesPath, OperatingSystem.IsWindows() ? "ffplay.exe" : "ffplay");
        if (!File.Exists(ffplayExe)) return null;

        var args = $"-nodisp -autoexit -i \"{videoPath}\"";
        var psi = new ProcessStartInfo { FileName = ffplayExe, Arguments = args, CreateNoWindow = true, UseShellExecute = false };
        return Process.Start(psi);
    }

    private static async Task FadeOpacityAsync(Control target, double from, double to, int ms, CancellationToken ct)
    {
        target.Opacity = from;
        const int steps = 30;
        var delay = TimeSpan.FromMilliseconds(ms / (double)steps);
        for (var i = 1; i <= steps; i++)
        {
            if (ct.IsCancellationRequested) break;
            var t = i / (double)steps;
            target.Opacity = from + (to - from) * t;
            await Task.Delay(delay, ct);
        }
        target.Opacity = to;
    }

    private async Task EnsureFfmpegReadyAsync()
    {
        // Check for system ffmpeg first (common on linux/vm)
        if (OperatingSystem.IsLinux())
        {
            if (File.Exists("/usr/bin/ffmpeg"))
            {
                FFmpeg.SetExecutablesPath("/usr/bin");
                return;
            }
        }

        var dir = OperatingSystem.IsWindows() 
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MetalLink", "ffmpeg")
            : Path.Combine(Path.GetTempPath(), "metallink_ffmpeg");
        
        Directory.CreateDirectory(dir);
        FFmpeg.SetExecutablesPath(dir);
        
        var ffmpeg = Path.Combine(dir, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
        if (!File.Exists(ffmpeg)) 
        {
            Console.WriteLine($"[INFO] Intro: Downloading FFmpeg to {dir}...");
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, dir);
        }
    }

    public void OnPointerPressed(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        Console.WriteLine("[INFO] Intro: Skip via click.");
        _playCts?.Cancel();
    }

    public void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Escape || e.Key == Avalonia.Input.Key.Enter || e.Key == Avalonia.Input.Key.Space)
        {
            Console.WriteLine($"[INFO] Intro: Skip via {e.Key}.");
            _playCts?.Cancel();
        }
    }
}
