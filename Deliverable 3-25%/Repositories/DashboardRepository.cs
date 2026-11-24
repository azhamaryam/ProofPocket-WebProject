using System.Data;
using Dapper;
using ProofPocket.ViewModels;

namespace ProofPocket.Repositories
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnection _connection;

        public DashboardRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<DashboardStatsViewModel> GetUserStatsAsync(
            string userId)
        {
            const string sql = @"
                SELECT
                    COUNT(*) AS TotalProducts,

                    SUM(CASE
                        WHEN DATEADD(MONTH, WarrantyMonths, PurchaseDate)
                             >= CAST(GETDATE() AS DATE)
                        THEN 1 ELSE 0
                    END) AS ActiveWarranties,

                    SUM(CASE
                        WHEN DATEADD(MONTH, WarrantyMonths, PurchaseDate)
                             BETWEEN CAST(GETDATE() AS DATE)
                             AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE))
                        THEN 1 ELSE 0
                    END) AS ExpiringSoon,

                    SUM(CASE
                        WHEN DATEADD(MONTH, WarrantyMonths, PurchaseDate)
                             < CAST(GETDATE() AS DATE)
                        THEN 1 ELSE 0
                    END) AS ExpiredWarranties

                FROM Products
                WHERE UserId = @UserId";

            return await _connection.QuerySingleAsync<DashboardStatsViewModel>(
                sql, new { UserId = userId });
        }

        public async Task<DashboardStatsViewModel> GetAdminStatsAsync()
        {
            const string sql = @"
                SELECT
                    (SELECT COUNT(*)
                     FROM AspNetUsers u
                     INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                     INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                     WHERE r.Name = 'User') AS TotalUsers,

                    (SELECT COUNT(*) FROM Categories) AS TotalCategories,

                    (SELECT COUNT(*) FROM Products) AS TotalProducts,

                    (SELECT COUNT(*)
                     FROM Products
                     WHERE DATEADD(MONTH, WarrantyMonths, PurchaseDate)
                           >= CAST(GETDATE() AS DATE)
                    ) AS ActiveWarranties";

            return await _connection.QuerySingleAsync<DashboardStatsViewModel>(
                sql);
        }
    }
}