using AISManager.Models;

namespace AISManager.Services
{
    public interface IHotfixService
    {
        Task<List<HotfixInfo>> GetHotfixesAsync(string version);
        Task DownloadHotfixAsync(HotfixInfo hotfix, string downloadPath, IProgress<int> progress, System.Threading.CancellationToken ct);
        Task<bool> ValidateHotfixAsync(HotfixInfo hotfix);
    }
}
