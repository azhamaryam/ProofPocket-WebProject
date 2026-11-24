using ProofPocket.ViewModels;

namespace ProofPocket.Repositories
{
    public interface IDashboardRepository
    {
        Task<DashboardStatsViewModel> GetUserStatsAsync(string userId);
        Task<DashboardStatsViewModel> GetAdminStatsAsync();
    }
}