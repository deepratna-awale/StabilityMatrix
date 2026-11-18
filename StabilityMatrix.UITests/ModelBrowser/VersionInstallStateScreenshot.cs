using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using StabilityMatrix.Avalonia.Services;
using StabilityMatrix.Avalonia.ViewModels;
using StabilityMatrix.Avalonia.ViewModels.Base;
using StabilityMatrix.Avalonia.ViewModels.CheckpointBrowser;
using StabilityMatrix.Avalonia.Views;
using StabilityMatrix.Core.Api;
using StabilityMatrix.Core.Helper.Factory;
using StabilityMatrix.Core.Models.Api;
using StabilityMatrix.Core.Services;

namespace StabilityMatrix.UITests.ModelBrowser;

[Collection("TempDir")]
public class VersionInstallStateScreenshot : TestBase
{
    private static CivitFile F(int id, string name, CivitFileType type, string hash, double kb, string fp) =>
        new()
        {
            Id = id,
            Name = name,
            Type = type,
            SizeKb = kb,
            Hashes = new CivitFileHashes { BLAKE3 = hash },
            Metadata = new CivitFileMetadata { Format = CivitModelFormat.SafeTensor, Fp = fp },
            DownloadUrl = "https://civitai.com/api/download/models/1",
        };

    private static CivitModelVersion V(int id, string name, int day, params CivitFile[] files) =>
        new()
        {
            Id = id,
            Name = name,
            BaseModel = "Anima",
            Description = "",
            TrainedWords = [],
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-day),
            Images = [],
            Stats = new CivitModelStats(),
            Files = files.ToList(),
        };

    private static CivitFile[] Files(int v, bool withModels = true) =>
        [
            F(v * 10 + 1, $"example_anima_v{v}_bf16.safetensors", CivitFileType.DiffusionModel, $"bf16-{v}", 4_000_000, "bf16"),
            F(v * 10 + 2, $"example_anima_v{v}_fp8.safetensors", CivitFileType.DiffusionModel, $"fp8-{v}", 2_000_000, "fp8"),
            F(v * 10 + 3, "anima_text_encoder_bf16.safetensors", CivitFileType.TextEncoder, "shared-encoder", 1_200_000, "bf16"),
            F(v * 10 + 4, "qwen_image_vae.safetensors", CivitFileType.VAE, "shared-vae", 250_000, "bf16"),
        ];

    [AvaloniaFact]
    public async Task Capture()
    {
        var model = new CivitModel
        {
            Id = 1,
            Name = "Example Anima Model",
            Type = CivitModelType.Checkpoint,
            Description = "",
            Tags = [],
            ModelVersions =
            [
                V(7, "v7.0 Turbo", 1, Files(7)),
                V(5, "v5.0", 2, Files(5)),
                V(4, "v4.2", 3, Files(4)),
                V(3, "v3.0", 4, Files(3)),
            ],
        };

        var modelIndex = Substitute.For<IModelIndexService>();
        modelIndex.ModelIndexBlake3Hashes.Returns(
            new HashSet<string> { "shared-encoder", "shared-vae", "fp8-3" }
        );

        var settings = Services.GetRequiredService<ISettingsManager>();
        if (!settings.IsLibraryDirSet)
            { settings.SetLibraryDirOverride(new StabilityMatrix.Core.Models.FileInterfaces.DirectoryPath(Path.Combine(TempDirFixture.ModuleTempDir, "Lib"))); settings.TryFindLibrary(); foreach (var d in new[]{"DiffusionModels","TextEncoders","VAE","StableDiffusion","Lora","ClipVision"}) Directory.CreateDirectory(Path.Combine(settings.ModelsDirectory, d)); }
        var vm = new CivitDetailsPageViewModel(
            settings,
            Services.GetRequiredService<CivitCompatApiManager>(),
            Substitute.For<ICivitTRPCApi>(),
            NullLogger<CivitDetailsPageViewModel>.Instance,
            Substitute.For<INotificationService>(),
            Substitute.For<INavigationService<MainWindowViewModel>>(),
            modelIndex,
            Services.GetRequiredService<IServiceManager<ViewModelBase>>(),
            Substitute.For<IModelImportService>(),
            Substitute.For<IDownloadService>(),
            Substitute.For<IPackageFactory>()
        )
        {
            CivitModel = model,
            ModelIdList = [1],
            CurrentIndex = 0,
        };

        var window = new Window
        {
            Width = 1400,
            Height = 900,
            Content = new CivitDetailsPage { DataContext = vm },
        };
        window.Show();
        await WaitHelper.WaitForConditionAsync(() => vm.ModelVersions.Count, c => c == 4);
        await Task.Delay(500);
        Dispatcher.UIThread.RunJobs();
        await Task.Delay(500);
        Dispatcher.UIThread.RunJobs();

        var dir = Environment.GetEnvironmentVariable("SM_SS_DIR")!;
        Directory.CreateDirectory(dir);
        using var bitmap = new RenderTargetBitmap(new PixelSize(1400 * 2, 900 * 2), new Vector(192, 192));
        bitmap.Render(window);
        bitmap.Save(Path.Combine(dir, Environment.GetEnvironmentVariable("SM_SS_NAME")!));
        window.Close();
    }
}
