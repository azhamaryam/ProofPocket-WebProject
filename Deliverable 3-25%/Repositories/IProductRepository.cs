using ProofPocket.Models;

namespace ProofPocket.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> GetByUserIdAsync(string userId);
        Task<Product?> GetByIdAsync(int id);
        Task<int> AddAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id, string userId);
    }
}