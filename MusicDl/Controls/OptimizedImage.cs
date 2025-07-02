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
    private static readonly object _cacheLock = new object();

    // 缓存配置
    private const int MAX_MEMORY_CACHE_SIZE = 100;
    private const int MAX_DISK_CACHE_SIZE_MB = 50; // 最大磁盘缓存50MB
    private const int CACHE_CLEANUP_THRESHOLD = 30; // 当缓存文件数量超过这个值时触发清理

    static OptimizedImage()
    {
        //_cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusicDl", "ImageCache");
        _cacheDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageCache");
        Directory.CreateDirectory(_cacheDirectory);

        // 应用启动时清理缓存
        _ = Task.Run(CleanupCacheAsync);
    }

    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl), typeof(string), typeof(OptimizedImage),
            new PropertyMetadata(null, OnImageUrlChanged));

    public string ImageUrl
    {
        get => (string)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public required BitmapImage DefaultImage { get; set; }

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
                // 更新文件访问时间
                File.SetLastAccessTime(filePath, DateTime.Now);
            }
            else
            {
                // 下载图片
                imageData = await _httpClient.GetByteArrayAsync(url);

                // 检查是否需要清理缓存
                await CheckAndCleanupCacheAsync();

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

            // 添加到内存缓存，使用LRU策略
            lock (_cacheLock)
            {
                if (_imageCache.Count >= MAX_MEMORY_CACHE_SIZE)
                {
                    // 移除最旧的缓存项
                    var oldKey = _imageCache.Keys.FirstOrDefault();
                    if (oldKey != null)
                        _imageCache.TryRemove(oldKey, out _);
                }
                _imageCache[url] = bitmap;
            }
        }
        catch (Exception)
        {
            // 保持默认图片
        }
    }

    private static async Task CheckAndCleanupCacheAsync()
    {
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.jpg");

            // 如果文件数量超过阈值或总大小超过限制，进行清理
            if (files.Length > CACHE_CLEANUP_THRESHOLD || await GetCacheSizeMBAsync() > MAX_DISK_CACHE_SIZE_MB)
            {
                await CleanupCacheAsync();
            }
        }
        catch (Exception)
        {
            // 忽略清理错误
        }
    }

    private static async Task CleanupCacheAsync()
    {
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.jpg")
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.LastAccessTime) // 按最后访问时间排序
                .ToArray();

            if (files.Length == 0) return;

            long totalSize = files.Sum(f => f.Length);
            long maxSizeBytes = MAX_DISK_CACHE_SIZE_MB * 1024 * 1024;

            // 删除最旧的文件直到大小在限制内
            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    if (totalSize <= maxSizeBytes && files.Length - Array.IndexOf(files, file) <= CACHE_CLEANUP_THRESHOLD)
                        break;

                    try
                    {
                        file.Delete();
                        totalSize -= file.Length;
                    }
                    catch (Exception)
                    {
                        // 忽略删除错误
                    }
                }
            });
        }
        catch (Exception)
        {
            // 忽略清理错误
        }
    }

    private static async Task<long> GetCacheSizeMBAsync()
    {
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.jpg");
            long totalSize = 0;

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    try
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                    catch (Exception)
                    {
                        // 忽略单个文件错误
                    }
                }
            });

            return totalSize / (1024 * 1024); // 转换为MB
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// 手动清理所有缓存
    /// </summary>
    public static async Task ClearAllCacheAsync()
    {
        try
        {
            // 清理内存缓存
            lock (_cacheLock)
            {
                _imageCache.Clear();
            }

            // 清理磁盘缓存
            await Task.Run(() =>
            {
                var files = Directory.GetFiles(_cacheDirectory, "*.jpg");
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception)
                    {
                        // 忽略删除错误
                    }
                }
            });
        }
        catch (Exception)
        {
            // 忽略清理错误
        }
    }

    /// <summary>
    /// 获取当前缓存信息
    /// </summary>
    public static async Task<(int FileCount, long SizeMB)> GetCacheInfoAsync()
    {
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.jpg");
            var sizeMB = await GetCacheSizeMBAsync();
            return (files.Length, sizeMB);
        }
        catch (Exception)
        {
            return (0, 0);
        }
    }

    private static string GetCacheFileName(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexString(hash) + ".jpg";
    }
}