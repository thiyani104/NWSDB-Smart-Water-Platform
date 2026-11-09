using NWSDB.API.DTOs;

namespace NWSDB.API.Data.Services;

public interface ICustomerService
{
    Task<WaterConnectionResponse?> GetConnectionByAccountAsync(string accountNumber);
    Task<List<WaterConnectionResponse>> GetConnectionsByUserIdAsync(int userId);
    Task<BillResponse?> GetCurrentBillAsync(string accountNumber);
}