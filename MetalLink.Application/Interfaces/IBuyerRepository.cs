using System.Collections.Generic;
using System.Threading.Tasks;
using MetalLink.Domain.Entities;

namespace MetalLink.Application.Interfaces;

public interface IBuyerRepository
{
    Task<Buyer?> GetByIdAsync(long buyerId);
    Task<Buyer?> GetByAccountNumberAsync(long accountNumber);
    Task<IEnumerable<Buyer>> GetAllAsync();
    Task<IEnumerable<Buyer>> SearchAsync(string searchTerm, int pageNumber = 1, int pageSize = 50);
    Task<Buyer> AddAsync(Buyer buyer);
    Task UpdateAsync(Buyer buyer);
}
