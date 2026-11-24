using ProofPocket.Models;

namespace ProofPocket.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category?> GetByIdAsync(int id);
        Task<int> AddAsync(Category category);
        Task<bool> UpdateAsync(Category category);
        Task<bool> DeleteAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasProductsAsync(int categoryId);
    }
}