using System.Data;
using Dapper;
using ProofPocket.Models;

namespace ProofPocket.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDbConnection _connection;

        public ProductRepository(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            const string sql = @"
                SELECT p.*, c.Id, c.Name, c.Description
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                ORDER BY p.Id DESC";

            return await _connection.QueryAsync<Product, Category, Product>(
                sql,
                (product, category) =>
                {
                    product.Category = category;
                    return product;
                },
                splitOn: "Id");
        }

        public async Task<IEnumerable<Product>> GetByUserIdAsync(string userId)
        {
            const string sql = @"
                SELECT p.*, c.Id, c.Name, c.Description
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.UserId = @UserId
                ORDER BY p.Id DESC";

            return await _connection.QueryAsync<Product, Category, Product>(
                sql,
                (product, category) =>
                {
                    product.Category = category;
                    return product;
                },
                new { UserId = userId },
                splitOn: "Id");
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            const string sql = @"
                SELECT p.*, c.Id, c.Name, c.Description
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.Id = @Id";

            var result = await _connection.QueryAsync<Product, Category, Product>(
                sql,
                (product, category) =>
                {
                    product.Category = category;
                    return product;
                },
                new { Id = id },
                splitOn: "Id");

            return result.FirstOrDefault();
        }

        public async Task<int> AddAsync(Product product)
        {
            const string sql = @"
                INSERT INTO Products
                (Name, CategoryId, Brand, Model, Store, Price,
                 PurchaseDate, WarrantyMonths, InvoiceNumber, UserId)
                VALUES
                (@Name, @CategoryId, @Brand, @Model, @Store, @Price,
                 @PurchaseDate, @WarrantyMonths, @InvoiceNumber, @UserId);

                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await _connection.ExecuteScalarAsync<int>(sql, product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            const string sql = @"
                UPDATE Products
                SET Name = @Name,
                    CategoryId = @CategoryId,
                    Brand = @Brand,
                    Model = @Model,
                    Store = @Store,
                    Price = @Price,
                    PurchaseDate = @PurchaseDate,
                    WarrantyMonths = @WarrantyMonths,
                    InvoiceNumber = @InvoiceNumber
                WHERE Id = @Id AND UserId = @UserId";

            int rows = await _connection.ExecuteAsync(sql, product);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id, string userId)
        {
            const string sql = @"
                DELETE FROM Products
                WHERE Id = @Id AND UserId = @UserId";

            int rows = await _connection.ExecuteAsync(
                sql, new { Id = id, UserId = userId });

            return rows > 0;
        }
    }
}