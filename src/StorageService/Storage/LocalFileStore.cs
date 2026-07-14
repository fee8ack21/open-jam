using Microsoft.Extensions.Options;
using StorageService.Options;

namespace StorageService.Storage;

/// <summary>
/// 地端本地檔案系統的底層讀寫操作，將物件鍵值映射為根目錄下的實體檔案路徑。
/// 同時供 <see cref="LocalStorageProvider"/>（刪除 / 存在檢查）與 blob 端點（上傳 / 下載）使用。
/// </summary>
public class LocalFileStore(IOptions<StorageOptions> options)
{
    private readonly string _root = Path.GetFullPath(options.Value.Local.RootPath);

    /// <summary>確保根目錄存在。</summary>
    public void EnsureRoot() => Directory.CreateDirectory(_root);

    /// <summary>
    /// 將內容串流寫入鍵值對應的檔案（建立必要的子目錄），回傳寫入的位元組數。
    /// <paramref name="maxBytes"/> 大於 0 時強制大小上限：超過即中止並刪除部分寫入的檔案、拋
    /// <see cref="InvalidOperationException"/>，避免超量直傳落地（即使 client 未帶或謊報 Content-Length）。
    /// </summary>
    public async Task<long> SaveAsync(string key, Stream content, CancellationToken ct, long maxBytes = 0)
    {
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        try
        {
            await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            var buffer = new byte[81920];
            long total = 0;
            int read;
            while ((read = await content.ReadAsync(buffer, ct)) > 0)
            {
                total += read;
                if (maxBytes > 0 && total > maxBytes)
                    throw new InvalidOperationException($"上傳內容超過大小上限（{maxBytes} bytes）。");
                await fs.WriteAsync(buffer.AsMemory(0, read), ct);
            }
            return total;
        }
        catch
        {
            // 超量 / 失敗時清掉部分寫入的檔案，不留半截孤兒。
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    /// <summary>開啟鍵值對應檔案的唯讀串流；檔案不存在時拋 <see cref="FileNotFoundException"/>。</summary>
    public Stream OpenRead(string key) =>
        new FileStream(Resolve(key), FileMode.Open, FileAccess.Read, FileShare.Read);

    /// <summary>檢查鍵值對應的檔案是否存在。</summary>
    public bool Exists(string key) => File.Exists(Resolve(key));

    /// <summary>刪除鍵值對應的檔案（冪等；不存在時不拋例外）。</summary>
    public void Delete(string key)
    {
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// 將物件鍵值解析為根目錄下的絕對路徑，並防範路徑穿越（key 含 <c>..</c> 逃逸根目錄）。
    /// </summary>
    private string Resolve(string key)
    {
        var normalized = key.Replace('\\', '/').TrimStart('/');
        var full = Path.GetFullPath(Path.Combine(_root, normalized));

        // 確保最終路徑仍位於根目錄內（Windows 檔案系統不分大小寫）。
        var rootWithSep = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;

        if (!full.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"非法的物件鍵值：{key}");

        return full;
    }
}
