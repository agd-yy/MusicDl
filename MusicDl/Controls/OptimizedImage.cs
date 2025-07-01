using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Text;

namespace MusicDl.Controls;

public class OptimizedImage : Image
{
    private static readonly ConcurrentDictionary<string, BitmapImage> _imageCache = new();
    private static readonly HttpClient _httpClient = new();
    private static readonly string _cacheDirectory;

    static OptimizedImage()
    {
        _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicDl", "ImageCache");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl), typeof(string), typeof(OptimizedImage),
            new PropertyMetadata(null, OnImageUrlChanged));

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public BitmapImage DefaultImage { get; set; }

    private static async void OnImageUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OptimizedImage control && e.NewValue is string url && !string.IsNullOrEmpty(url))
        {
            await control.LoadImageAsync(url);
        }
    }

    private async Task LoadImageAsync(string url)
    {
        // 设置默认图片
        Source = DefaultImage;

        try
        {
            // 检查内存缓存
            if (_imageCache.TryGetValue(url, out var cachedImage))
            {
                Source = cachedImage;
                return;
            }

            // 生成缓存文件名
            var fileName = GetCacheFileName(url);
            var filePath = Path.Combine(_cacheDirectory, fileName);

            byte[] imageData;

            // 检查磁盘缓存
            if (File.Exists(filePath))
            {
                imageData = await File.ReadAllBytesAsync(filePath);
            }
            else
            {
                // 下载图片
                imageData = await _httpClient.GetByteArrayAsync(url);

                // 保存到磁盘缓存
                await File.WriteAllBytesAsync(filePath, imageData);
            }

            // 创建优化的BitmapImage
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 60; // 限制解码尺寸
            bitmap.DecodePixelHeight = 60;
            bitmap.StreamSource = new MemoryStream(imageData);
            bitmap.EndInit();
            bitmap.Freeze();

            // 更新UI
            Dispatcher.Invoke(() => Source = bitmap);

            // 添加到缓存
            if (_imageCache.Count > 100)
            {
                var oldKey = _imageCache.Keys.FirstOrDefault();
                if (oldKey != null)
                    _imageCache.TryRemove(oldKey, out _);
            }
            _imageCache[url] = bitmap;
        }
        catch (Exception)
        {
            // 保持默认图片
        }
    }

    private static string GetCacheFileName(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(hash) + ".jpg";
    }
}