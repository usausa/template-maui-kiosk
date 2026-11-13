namespace Template.MobileApp.Components;

public enum AppUpdateResult
{
    Downloaded,
    UpToDate,
    InvalidPackage,
    DownloadFailed,
    InstallFailed
}

public sealed partial class AppUpdater : IDisposable
{
    private const string FileName = "update.apk";

    private static readonly TimeSpan InstallTimeout = TimeSpan.FromSeconds(60);

    private readonly IFileSystem fileSystem;

    private readonly HttpClient client = new();

    private TaskCompletionSource? installation;

    private string PackagePath => Path.Combine(fileSystem.CacheDirectory, FileName);

    public AppUpdater(IFileSystem fileSystem)
    {
        this.fileSystem = fileSystem;
    }

    public void Dispose() => client.Dispose();

    // ------------------------------------------------------------
    // Download
    // ------------------------------------------------------------

    public async Task<AppUpdateResult> DownloadAsync(string location)
    {
        if (!Uri.TryCreate(location, UriKind.Absolute, out var uri))
        {
            return AppUpdateResult.DownloadFailed;
        }

        try
        {
            await using var input = await client.GetStreamAsync(uri);
            await using var output = File.Create(PackagePath);
            await input.CopyToAsync(output);
        }
        catch (HttpRequestException)
        {
            File.Delete(PackagePath);
            return AppUpdateResult.DownloadFailed;
        }
        catch (IOException)
        {
            File.Delete(PackagePath);
            return AppUpdateResult.DownloadFailed;
        }

        var result = PlatformCheckPackage(PackagePath);
        if (result != AppUpdateResult.Downloaded)
        {
            File.Delete(PackagePath);
        }

        return result;
    }

    // ------------------------------------------------------------
    // Install
    // ------------------------------------------------------------

    public async Task<AppUpdateResult> InstallAsync()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        installation = completion;
        try
        {
            if (await PlatformInstallAsync(PackagePath))
            {
                await completion.Task.WaitAsync(InstallTimeout);
            }

            return AppUpdateResult.InstallFailed;
        }
        finally
        {
            installation = null;
            File.Delete(PackagePath);
        }
    }

    public void NotifyInstallFailed() => installation?.TrySetResult();

    private static partial AppUpdateResult PlatformCheckPackage(string path);

    private static partial Task<bool> PlatformInstallAsync(string path);
}
