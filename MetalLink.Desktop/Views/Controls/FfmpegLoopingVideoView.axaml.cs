using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Downloader;

namespace MetalLink.Desktop.Views.Controls;

public partial class FfmpegLoopingVideoView : UserControl
{
    public static readonly StyledProperty<Avalonia.Media.Stretch> StretchModeProperty =
        AvaloniaProperty.Register<FfmpegLoopingVideoView, Avalonia.Media.Stretch>(
            nameof(StretchMode),
            Avalonia.Media.Stretch.UniformToFill);

    public Avalonia.Media.Stretch StretchMode
    {
        get => GetValue(StretchModeProperty);
        set => SetValue(StretchModeProperty, value);
    }

    public event EventHandler? FirstFrameRendered;
    private bool _hasRenderedFirstFrame;
    public static readonly StyledProperty<Uri?> SourceProperty =
        AvaloniaProperty.Register<FfmpegLoopingVideoView, Uri?>(nameof(Source));

    public Uri? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    private Image? _frame;
    private CancellationTokenSource? _cts;

    public FfmpegLoopingVideoView()
    {
        InitializeComponent();
        _frame = this.FindControl<Image>("Frame");

        this.GetObservable(SourceProperty).Subscribe(uri =>
        {
            Stop();
            if (uri != null)
                Start(uri);
        });

        DetachedFromVisualTree += (_, __) => Stop();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void Stop()
    {
        try { _cts?.Cancel(); } catch { }
        _cts?.Dispose();
        _cts = null;
        _hasRenderedFirstFrame = false;
    }

    private void Start(Uri uri)
    {
        _cts = new CancellationTokenSource();
        _ = RunDecodeLoopAsync(uri, _cts.Token);
    }

    private static async Task EnsureFfmpegAsync()
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

        // Fallback: Downloads FFmpeg binaries on first run into a cache folder.
        // Works cross-distro without system ffmpeg.
        var dir = Path.Combine(Path.GetTempPath(), "metallink_ffmpeg");
        Directory.CreateDirectory(dir);
        FFmpeg.SetExecutablesPath(dir);

        if (!File.Exists(Path.Combine(dir, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg")))
        {
            await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, dir);
        }
    }

    private async Task RunDecodeLoopAsync(Uri uri, CancellationToken ct)
    {
        try
        {
            await EnsureFfmpegAsync();

            var inputPath = ResolveInputPath(uri);

            // Extract frames for the *entire* clip duration, then loop them in-memory.
            // This guarantees the animation is embedded and works regardless of distro.
            const int fps = 30;

            var mediaInfo = await FFmpeg.GetMediaInfo(inputPath);

            // Prefer per-stream duration (container duration is sometimes wrong)
            var duration = mediaInfo.Duration;
            foreach (var vs in mediaInfo.VideoStreams)
            {
                if (vs.Duration > duration)
                    duration = vs.Duration;
            }

            if (duration <= TimeSpan.Zero)
                duration = TimeSpan.FromSeconds(7);

            // small buffer to avoid cutting off the last frames due to timestamp rounding
            duration += TimeSpan.FromMilliseconds(250);

            var frameCount = (int)Math.Ceiling(duration.TotalSeconds * fps);
            if (frameCount < 1)
                frameCount = fps * 7;

            // Use a stable cache directory based on video file
            // Reuse frames across restarts - don't extract every time
            var fileInfo = new FileInfo(inputPath);
            var fileName = Path.GetFileNameWithoutExtension(inputPath);
            var framesDir = Path.Combine(Path.GetTempPath(), $"metallink_frames_{fileName}_{fileInfo.Length}");
            Directory.CreateDirectory(framesDir);

            var outputPattern = Path.Combine(framesDir, "frame_%05d.png");

            // Check if we already have enough frames cached
            var existingFrames = Directory.GetFiles(framesDir, "frame_*.png");
            var needsExtraction = existingFrames.Length < frameCount * 0.8;

            if (!needsExtraction)
            {
                System.Console.Error.WriteLine($"[INFO] Using {existingFrames.Length} cached frames for {fileName}");
            }
            else
            {
                System.Console.Error.WriteLine($"[INFO] Extracting frames for {fileName} (need ~{frameCount})");
                // Delete old/incomplete frames
                foreach (var f in existingFrames)
                {
                    try { File.Delete(f); } catch { }
                }
            }

            // Extract frames only if needed
            if (needsExtraction)
            {
                var exeDir = FFmpeg.ExecutablesPath;
                var ffmpeg = Path.Combine(exeDir, OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");
                
                var durationSeconds = duration.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
                var args = $"-y -i \"{inputPath}\" -t {durationSeconds} -vf fps={fps},scale=256:-1:flags=lanczos -frames:v {frameCount} \"{outputPattern}\"";
                
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                
                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    // Read output streams immediately in background to avoid deadlock
                    var errorTask = process.StandardError.ReadToEndAsync();
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    
                    var completed = await Task.WhenAny(process.WaitForExitAsync(), Task.Delay(180000, ct));
                    if (completed != process.WaitForExitAsync())
                    {
                        try { if (!process.HasExited) process.Kill(true); } catch { }
                        throw new TimeoutException("Frame extraction timed out after 180 seconds");
                    }
                    
                    if (process.ExitCode != 0)
                    {
                        var err = await errorTask;
                        throw new InvalidOperationException($"ffmpeg failed with code {process.ExitCode}: {err}");
                    }
                }
            }

            // Wait for all frames to be written and accessible
            await EnsureFramesAccessibleAsync(framesDir, frameCount);

            var files = new List<string>(Directory.GetFiles(framesDir, "frame_*.png"));
            files.Sort(StringComparer.Ordinal);

            if (files.Count == 0)
            {
                System.Console.Error.WriteLine($"[ERROR] No frames extracted. Expected ~{frameCount} frames in {framesDir}");
                return;
            }

            // Load bitmaps once - copy to memory stream to keep data alive after file stream closes
            var bitmaps = new List<Bitmap>(files.Count);
            foreach (var f in files)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    // Read entire file into memory to keep data alive
                    var data = await File.ReadAllBytesAsync(f, ct);
                    var ms = new MemoryStream(data, writable: false);
                    var bitmap = new Bitmap(ms);
                    bitmaps.Add(bitmap);
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine($"[WARN] Failed to load frame {f}: {ex.Message}");
                }
            }

            if (bitmaps.Count == 0)
            {
                System.Console.Error.WriteLine("[ERROR] Failed to load any frames into memory");
                return;
            }

            var delay = TimeSpan.FromMilliseconds(1000.0 / fps);
            var idx = 0;

            while (!ct.IsCancellationRequested)
            {
                var bmp = bitmaps[idx];
                idx = (idx + 1) % bitmaps.Count;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (_frame is not null)
                    {
                        _frame.Source = bmp;
                        if (!_hasRenderedFirstFrame)
                        {
                            _hasRenderedFirstFrame = true;
                            FirstFrameRendered?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }, DispatcherPriority.Render);

                await Task.Delay(delay, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // normal
        }
        catch (Exception ex)
        {
            System.Console.Error.WriteLine($"[WARN] Video playback failed: {ex.Message}");
        }
    }

    private static async Task EnsureFramesAccessibleAsync(string framesDir, int expectedFrameCount)
    {
        // Wait for all frames to be fully written and accessible by the file system
        const int maxRetries = 100;
        const int retryDelayMs = 50;
        var minFrames = Math.Max(5, (int)(expectedFrameCount * 0.9));

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var files = Directory.GetFiles(framesDir, "frame_*.png");
                if (files.Length >= minFrames)
                {
                    // Try to open each file to verify they're fully written and accessible
                    var allAccessible = true;
                    foreach (var f in files)
                    {
                        try
                        {
                            using var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.Read);
                            // Just checking if we can open it is enough
                        }
                        catch (IOException)
                        {
                            allAccessible = false;
                            break;
                        }
                    }

                    if (allAccessible)
                        return; // All frames are accessible
                }
            }
            catch
            {
                // Retry
            }

            await Task.Delay(retryDelayMs);
        }

        // If we get here, log a warning but continue anyway (frames should be accessible soon)
        System.Console.Error.WriteLine($"[WARN] Timeout waiting for frames to be accessible in {framesDir}");
    }

    private static string ResolveInputPath(Uri source)
    {
        if (source.Scheme.Equals("avares", StringComparison.OrdinalIgnoreCase))
        {
            using var stream = AssetLoader.Open(source);
            var fileName = Path.GetFileName(source.AbsolutePath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "logo.mp4";

            var tempPath = Path.Combine(Path.GetTempPath(), $"metallink_{fileName}");
            using var fs = File.Create(tempPath);
            stream.CopyTo(fs);
            return tempPath;
        }

        if (source.IsFile)
            return source.LocalPath;

        return source.ToString();
    }
}
