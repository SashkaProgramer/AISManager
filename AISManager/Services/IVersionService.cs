namespace AISManager.Services
{
    public interface IVersionService
    {
        Task<string> GetCurrentAISVersionAsync();
        Task<bool> ValidateVersionAsync(string version);
    }
}
