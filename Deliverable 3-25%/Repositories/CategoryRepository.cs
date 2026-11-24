using System.Data;
using Dapper;
using ProofPocket.Models;

namespace ProofPocket.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbConnection _connection;

        public CategoryRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            const string sql = @"
                SELECT Id, Name, Description
                FROM Categories
                ORDER BY Name";

            return await _connection.QueryAsync<Category>(sql);
        }

        public async Task<Category?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT Id, Name, Description
                FROM Categories
                WHERE Id = @Id";

            return await _connection.QueryFirstOrDefaultAsync<Category>(
                sql, new { Id = id });
        }

        public async Task<int> AddAsync(Category category)
        {
            const string sql = @"
                INSERT INTO Categories (Name, Description)
                VALUES (@Name, @Description);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _connection.ExecuteScalarAsync<int>(sql, category);
        }

        public async Task<bool> UpdateAsync(Category category)
        {
            const string sql = @"
                UPDATE Categories
                SET Name = @Name,
                    Description = @Description
                WHERE Id = @Id";

            int rows = await _connection.ExecuteAsync(sql, category);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            const string sql = "DELETE FROM Categories WHERE Id = @Id";

            int rows = await _connection.ExecuteAsync(sql, new { Id = id });
            return rows > 0;
        }

        public async Task<bool> ExistsByNameAsync(
            string name, int? excludeId = null)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Categories
                WHERE LOWER(Name) = LOWER(@Name)
                  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)";

            int count = await _connection.ExecuteScalarAsync<int>(
                sql, new { Name = name, ExcludeId = excludeId });

            return count > 0;
        }

        public async Task<bool> HasProductsAsync(int categoryId)
        {
            const string sql = @"
                SELECT COUNT(1)
                FROM Products
                WHERE CategoryId = @CategoryId";

            int count = await _connection.ExecuteScalarAsync<int>(
                sql, new { CategoryId = categoryId });

            return count > 0;
        }
    }
}