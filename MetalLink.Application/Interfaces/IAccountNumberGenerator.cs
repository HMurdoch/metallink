using System.Threading;
using System.Threading.Tasks;

namespace MetalLink.Application.Interfaces;

public interface IAccountNumberGenerator
{
    /// <summary>
    /// Returns the next globally unique account number across BOTH customers and buyers.
    /// </summary>
    Task<long> GetNextAsync(CancellationToken ct = default);
}
