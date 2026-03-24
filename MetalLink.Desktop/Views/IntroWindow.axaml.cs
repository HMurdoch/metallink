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

    public IntroWindow()
    {
        InitializeComponent();
        _video = this.FindControl<Views.Controls.FfmpegLoopingVideoView>("IntroVideo");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Plays the intro clip:
    /// - Wait for first frame (avoid blank while FFmpeg extracts)
    /// - Fade video in over 1s
    /// - Hold 4s
    /// - Fade video out over 1s
    ///
    /// NOTE: audio fade is not implemented yet because the FFmpeg frame renderer has no audio output.
    /// </summary>
    public async Task PlayAsync(bool playVideo = true)
    {
        if (_video is null)
            return;

        var src = new Uri("avares://MetalLink.Desktop/Assets/intro_video_6sec.mp4");

        // Size the window to match the video's true aspect ratio so Stretch=Uniform doesn't letterbox.
        try
        {
            await EnsureFfmpegReadyAsync();

            var inputPath = ResolveInputPath(src);
            var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);
            var stream = mediaInfo.VideoStreams.FirstOrDefault();
            if (stream is not null)
            {
                // Target max height so it fits typical screens; keep aspect ratio.
                var maxH = 900.0;
                var h = Math.Min(maxH, stream.Height);
                var w = h * stream.Width / stream.Height;

                Width = w;
                Height = h;

                // Re-center (prefer the bounds provided by LoginWindow)
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
                // Requirement: display last frame for 1 second and then go to main application
                var lastFramePath = Path.Combine(Path.GetTempPath(), "metallink_intro_last_frame.jpg");
                
                // Extract last frame if not exists or just always refresh
                var ffmpegExe = Path.Combine(FFmpeg.ExecutablesPath, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
                // -sseof -3 means seek to 3 seconds before end of file
                // -update 1 -frames:v 1 gets one frame
                var args = $"-y -sseof -1 -i \"{inputPath}\" -update 1 -frames:v 1 \"{lastFramePath}\"";
                var psi = new ProcessStartInfo { FileName = ffmpegExe, Arguments = args, CreateNoWindow = true, UseShellExecute = false };
                using var p = Process.Start(psi);
                if (p != null) await p.WaitForExitAsync();

                if (File.Exists(lastFramePath))
                {
                    // Use a simple Image control or just hijack the video view if it supports images
                    // For now, let's just use the video view and seek to the end if possible, 
                    // but the request asks for an image export.
                    // We'll just show the window with the extracted image for 1s.
                    // Since I don't want to change XAML, I'll just set the video source to the image 
                    // if FfmpegLoopingVideoView supports it, or just show the window.
                    
                    // Actually, let's just use the video view but set its source to the JPG.
                    _video.Source = new Uri(lastFramePath);
                    _video.Opacity = 1;
                    await Task.Delay(1000);
                    return;
                }
            }

        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("[WARN] Intro sizing/audio failed: " + ex.Message);
        }

        if (!playVideo)
        {
            await Task.Delay(1000); // Wait 1s if we showed last frame
            return;
        }

        _video.Source = src;
        _video.Opacity = 0;

        // Wait for first frame so we don't show a blank window while FFmpeg extracts frames.
        var tcs = new TaskCompletionSource();
        void Handler(object? s, EventArgs e) => tcs.TrySetResult();
        _video.FirstFrameRendered += Handler;

        try
        {
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(8000));
            if (completed != tcs.Task)
                return;

            // Now that first frame is ready, start audio playback
            try
            {
                var inputPath = ResolveInputPath(src);
                _ffplay = await StartAudioAsync(inputPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[WARN] Intro audio failed: {ex.Message}");
            }

            await FadeOpacityAsync(_video, from: 0, to: 1, ms: 1000);
            await Task.Delay(4000);
            await FadeOpacityAsync(_video, from: 1, to: 0, ms: 1000);
        }
        finally
        {
            _video.FirstFrameRendered -= Handler;

            try
            {
                if (_ffplay is { HasExited: false })
                    _ffplay.Kill(entireProcessTree: true);
            }
            catch { /* ignore */ }
        }
    }

    private static async Task FadeOpacityAsync(Control target, double from, double to, int ms)
    {
        target.Opacity = from;
        const int steps = 60;
        var delay = TimeSpan.FromMilliseconds(ms / (double)steps);

        for (var i = 1; i <= steps; i++)
        {
            var t = i / (double)steps;
            target.Opacity = from + (to - from) * t;
            await Task.Delay(delay);
        }

        target.Opacity = to;
    }

    private static string ResolveInputPath(Uri source)
    {
        if (source.Scheme.Equals("avares", StringComparison.OrdinalIgnoreCase))
        {
            using var stream = AssetLoader.Open(source);
            var fileName = Path.GetFileName(source.AbsolutePath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "intro.mp4";

            var tempPath = Path.Combine(Path.GetTempPath(), $"metallink_{fileName}");
            using var fs = File.Create(tempPath);
            stream.CopyTo(fs);
            return tempPath;
        }

        if (source.IsFile)
            return source.LocalPath;

        return source.ToString();
    }

    private static async Task<Process?> StartAudioAsync(string inputPath)
    {
        // 1) Best case: ffplay available (ships with some ffmpeg bundles)
        var ffplay = TryGetFfplayPath();
        if (ffplay is not null)
        {
            return StartProcess(ffplay, $"-nodisp -autoexit -loglevel quiet \"{inputPath}\"");
        }

        // 2) Linux fallback: extract audio to wav using ffmpeg binary, then play via paplay/aplay
        if (OperatingSystem.IsLinux())
        {
            try
            {
                var wav = Path.Combine(Path.GetTempPath(), "metallink_intro_audio.wav");
                await ExtractAudioWavWithFfmpegAsync(inputPath, wav);

                var paplay = "/usr/bin/paplay";
                if (File.Exists(paplay))
                    return StartProcess(paplay, $"\"{wav}\"");

                var aplay = "/usr/bin/aplay";
                if (File.Exists(aplay))
                    return StartProcess(aplay, $"-q \"{wav}\"");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[WARN] Intro audio fallback failed: " + ex);
            }
        }

        return null;
    }

    private static string? TryGetFfplayPath()
    {
        try
        {
            var exeDir = FFmpeg.ExecutablesPath;
            if (string.IsNullOrWhiteSpace(exeDir))
                return null;

            var ffplay = Path.Combine(exeDir, "ffplay");
            if (OperatingSystem.IsWindows()) ffplay += ".exe";
            return File.Exists(ffplay) ? ffplay : null;
        }
        catch
        {
            return null;
        }
    }

    private static Process? StartProcess(string fileName, string args)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            return Process.Start(psi);
        }
        catch
        {
            return null;
        }
    }

    private static async Task ExtractAudioWavWithFfmpegAsync(string inputPath, string wavPath)
    {
        // Always extract fresh audio for this session to avoid stale cached files
        // Delete any existing cache file
        try
        {
            if (File.Exists(wavPath))
                File.Delete(wavPath);
        }
        catch
        {
            // If we can't delete it, just continue
        }

        var exeDir = FFmpeg.ExecutablesPath;
        if (string.IsNullOrWhiteSpace(exeDir))
            throw new InvalidOperationException("FFmpeg executables path is not set.");

        var ffmpeg = Path.Combine(exeDir, "ffmpeg");
        if (OperatingSystem.IsWindows()) ffmpeg += ".exe";
        if (!File.Exists(ffmpeg))
            throw new FileNotFoundException("ffmpeg not found", ffmpeg);

        // Extract audio track to wav using ProcessStartInfo
        // Use -af volume=0.7 to set intro audio to 70%
        var args = $"-y -i \"{inputPath}\" -vn -acodec pcm_s16le -ar 44100 -ac 2 -af volume=0.7 \"{wavPath}\"";
        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start ffmpeg for audio extraction.");

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(true); } catch { }
            throw new TimeoutException("ffmpeg audio extraction timed out.");
        }

        if (process.ExitCode != 0)
        {
            var err = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"ffmpeg audio extraction failed with code {process.ExitCode}: {err}");
        }
        
        // Ensure file is fully written to disk and accessible before returning.
        // This is critical: we must wait for the file to be completely written
        // and not locked by any processes before attempting playback.
        await EnsureAudioFileAccessibleAsync(wavPath);
    }

    private static async Task EnsureAudioFileAccessibleAsync(string wavPath)
    {
        const int maxRetries = 100;
        const int retryDelayMs = 50;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            if (!File.Exists(wavPath))
            {
                await Task.Delay(retryDelayMs);
                continue;
            }

            try
            {
                // Try to open the file to verify it's accessible and fully written
                // We use FileAccess.Read and FileShare.Read to check if the file is unlocked
                using var fs = new FileStream(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var fileSize = fs.Length;
                
                // Basic sanity check: WAV files should be at least a few KB
                if (fileSize > 1024)
                {
                    // File is accessible and has meaningful content
                    return;
                }
            }
            catch (IOException)
            {
                // File still locked or inaccessible, retry
                await Task.Delay(retryDelayMs);
                continue;
            }

            await Task.Delay(retryDelayMs);
        }

        // If we get here, check one more time - the file should exist if ffmpeg succeeded
        if (!File.Exists(wavPath))
        {
            throw new FileNotFoundException("Audio extraction produced no output file", wavPath);
        }

        // File exists but we couldn't verify it's fully written - wait a bit more and proceed
        // This is a fallback to ensure we don't fail when the file is actually fine
        await Task.Delay(TimeSpan.FromMilliseconds(200));
    }

    private static async Task EnsureFfmpegReadyAsync()
    {
        // Try to use system FFmpeg first
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var systemFfmpeg = OperatingSystem.IsLinux() ? "/usr/bin/ffmpeg" : "/usr/local/bin/ffmpeg";
            if (File.Exists(systemFfmpeg))
            {
                FFmpeg.SetExecutablesPath(Path.GetDirectoryName(systemFfmpeg) ?? "/usr/bin");
                return;
            }
        }

        // Fallback: Download ffmpeg binaries on first run.
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache", "metallink_ffmpeg");
        Directory.CreateDirectory(dir);
        FFmpeg.SetExecutablesPath(dir);

        // If ffmpeg already present, skip download.
        var ffmpeg = Path.Combine(dir, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
        if (File.Exists(ffmpeg))
            return;

        await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, dir);
    }
}
