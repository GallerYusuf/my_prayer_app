using System.Threading.Tasks;

namespace PrayerApp.Core.Contracts;

public interface IBackupService
{
    Task<string> ExportBackupAsync();
    Task<bool> ImportBackupAsync(string jsonContent);
}
