using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StabilityMatrix.Avalonia.Models;
using StabilityMatrix.Avalonia.ViewModels.Dialogs;
using StabilityMatrix.Avalonia.Views.Dialogs;
using StabilityMatrix.Core.Models.Database;
using StabilityMatrix.Core.Services;

namespace StabilityMatrix.UITests;

/// <summary>
/// Throwaway: renders the image viewer with its info panel open for every image in
/// SCREENSHOT_INPUT_DIR and saves "shot-NAME.png" to SCREENSHOT_OUTPUT_DIR (defaults to the input dir).
/// Run: dotnet test StabilityMatrix.UITests --filter FullyQualifiedName~GenerationDataScreenshots
/// </summary>
[Collection("TempDir")]
public class GenerationDataScreenshots
{
    [AvaloniaFact]
    public void Capture()
    {
        var input = Environment.GetEnvironmentVariable("SCREENSHOT_INPUT_DIR");
        if (string.IsNullOrEmpty(input) || !Directory.Exists(input))
            return;
        var output = Environment.GetEnvironmentVariable("SCREENSHOT_OUTPUT_DIR") ?? input;
        Directory.CreateDirectory(output);

        var images = Directory
            .EnumerateFiles(input)
            .Where(f => LocalImageFile.SupportedImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Where(f => !Path.GetFileName(f).StartsWith("shot-"));

        foreach (var path in images)
        {
            var vm = new ImageViewerViewModel(
                NullLogger<ImageViewerViewModel>.Instance,
                Substitute.For<ISettingsManager>()
            )
            {
                ImageSource = new ImageSource(path),
                LocalImageFile = LocalImageFile.FromPath(path),
            };
            var view = new ImageViewerDialog { DataContext = vm };
            var window = new Window { Width = 1280, Height = 900, Content = view };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            view.FindControl<TeachingTip>("InfoTeachingTip")!.IsOpen = true;
            for (var i = 0; i < 10; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }

            window
                .CaptureRenderedFrame()
                ?.Save(Path.Combine(output, "shot-" + Path.GetFileNameWithoutExtension(path) + ".png"));
            window.Close();
        }
    }
}
