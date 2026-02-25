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
        // Downloads FFmpeg binaries on first run into a cache folder.
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

            // Cache frames per input file size (cheap fingerprint)
            var framesDir = Path.Combine(Path.GetTempPath(), $"metallink_logo_frames_{new FileInfo(inputPath).Length}");
            Directory.CreateDirectory(framesDir);

            var outputPattern = Path.Combine(framesDir, "frame_%05d.png");

            // Regenerate frames if missing or if the cached set is clearly shorter than expected.
            var existingCount = Directory.GetFiles(framesDir, "frame_*.png").Length;
            if (existingCount < Math.Max(5, (int)(frameCount * 0.9)))
            {
                foreach (var f in Directory.GetFiles(framesDir, "frame_*.png"))
                {
                    try { File.Delete(f); } catch { }
                }

                // Note: parameters are passed as a single raw argument string.
                // -t ensures we don't cut the clip early; -frames:v is a safety cap.
                var args = $"-y -i \"{inputPath}\" -t {duration.TotalSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture)} -vf fps={fps},scale=256:-1:flags=lanczos -frames:v {frameCount} \"{outputPattern}\"";
                var conversion = FFmpeg.Conversions.New();
                conversion.AddParameter(args, ParameterPosition.PreInput);
                await conversion.Start(ct);
            }

            var files = new List<string>(Directory.GetFiles(framesDir, "frame_*.png"));
            files.Sort(StringComparer.Ordinal);

            if (files.Count == 0)
                return;

            // Load bitmaps once.
            var bitmaps = new List<Bitmap>(files.Count);
            foreach (var f in files)
            {
                ct.ThrowIfCancellationRequested();
                await using var fs = File.OpenRead(f);
                bitmaps.Add(new Bitmap(fs));
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
            System.Console.Error.WriteLine("[WARN] FFmpeg logo decode failed: " + ex);
        }
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
