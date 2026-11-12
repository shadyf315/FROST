using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FROST.Services;

public static class BackgroundImageService
{
    private const string ImageUrl =
        "https://lifehacker.ru/special/new-year/images/4db0f493-66ea-4534-8b87-8b03df68939d__depositphotos_7209166_m-2015.jpg";

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    static BackgroundImageService()
    {
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("FROST-Windows-Desktop/1.0");
    }

    public static async Task<ImageSource?> LoadAsync()
    {
        var cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FROST",
            "Cache");

        var cachePath = Path.Combine(cacheDirectory, "new-year-background.jpg");
        var temporaryPath = cachePath + ".download";

        try
        {
            Directory.CreateDirectory(cacheDirectory);

            if (!File.Exists(cachePath))
            {
                using var response = await Client.GetAsync(
                    ImageUrl,
                    HttpCompletionOption.ResponseHeadersRead);

                response.EnsureSuccessStatusCode();

                await using (var source = await response.Content.ReadAsStreamAsync())
                await using (var destination = new FileStream(
                    temporaryPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true))
                {
                    await source.CopyToAsync(destination);
                }

                File.Move(temporaryPath, cachePath, overwrite: true);
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(cachePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();

            return image;
        }
        catch
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // Ошибка очистки временного файла не должна влиять на запуск приложения.
            }

            return null;
        }
    }
}