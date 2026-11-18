using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using StabilityMatrix.Avalonia.Controls;
using StabilityMatrix.Avalonia.ViewModels;
using StabilityMatrix.Avalonia.ViewModels.Dialogs;
using StabilityMatrix.Avalonia.Views;
using StabilityMatrix.Avalonia.Views.Dialogs;
using StabilityMatrix.Core.Models;
using StabilityMatrix.Core.Services;

namespace StabilityMatrix.UITests;

[Collection("TempDir")]
public class ScratchScreenshotTests
{
    private static string OutDir => Environment.GetEnvironmentVariable("SM_SHOTS_DIR")!;
    private static string LibDir => Environment.GetEnvironmentVariable("SM_SHOTS_LIB")!;

    private static async Task Pump(int ms)
    {
        var end = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < end)
        {
            global::Avalonia.Headless.AvaloniaHeadlessPlatform.ForceRenderTimerTick(5);
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(50);
        }
    }

    private static void Save(Window window, string name)
    {
        window.CaptureRenderedFrame()!.Save(Path.Combine(OutDir, name));
    }

    [AvaloniaFact]
    public async Task CheckpointManager()
    {
        var settings = TestBase.Services.GetRequiredService<ISettingsManager>();
        settings.SetLibraryDirOverride(LibDir);
        settings.TryFindLibrary(true);
        settings.Transaction(s => s.CheckpointsPageResizeFactor = 1, ignoreMissingLibraryDir: true);

        var index = TestBase.Services.GetRequiredService<IModelIndexService>();
        await index.RefreshIndex();

        var vm = TestBase.Services.GetRequiredService<CheckpointsPageViewModel>();
        var view = new CheckpointsPage { DataContext = vm };
        vm.ResizeFactor = 1;
        var window = new Window { Width = 1720, Height = 720, Content = view };
        window.Show();
        await vm.OnLoadedAsync();
        await Pump(2000);
        foreach (var c in window.GetVisualDescendants().OfType<Layoutable>())
            c.InvalidateMeasure();
        await Pump(2000);
        Save(window, Environment.GetEnvironmentVariable("SM_SHOTS_NAME") ?? "checkpoint-manager.png");
        window.Close();
    }

    [AvaloniaFact]
    public async Task MetadataDialog()
    {
        var path = Environment.GetEnvironmentVariable("SM_SHOTS_META_FILE")
            ?? Path.Combine(LibDir, "Models", "Lora", "soft_watercolor_v2.safetensors");
        var vm = TestBase.Services.GetRequiredService<SafetensorMetadataViewModel>();
        vm.ModelName = Environment.GetEnvironmentVariable("SM_SHOTS_META_TITLE") ?? Path.GetFileNameWithoutExtension(path);
        vm.Metadata = await SafetensorMetadata.ParseAsync(path);
        var window = new Window { Width = 820, Height = 860, Content = new Grid() };
        var host = new BetterContentDialog
        {
            Content = new SafetensorMetadataDialog { DataContext = vm },
            CloseButtonText = "Close",
            MinDialogWidth = 700,
            MinDialogHeight = 800,
        };
        window.Show();
        var showTask = host.ShowAsync(window);
        await Pump(1500);
        Save(window, Environment.GetEnvironmentVariable("SM_SHOTS_META_NAME") ?? "view-metadata.png");
        host.Hide();
        await Pump(300);
        await showTask;
        window.Close();
    }
}
