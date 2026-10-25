using NWSDB.API.DTOs;

namespace NWSDB.API.Data.Services;

public interface IBillingService
{
    Task<MeterReadingResponse?> RecordMeterReadingAsync(MeterReadingRequest request);
    Task<List<BillResponse>> GetBillsByAccountAsync(string accountNumber);
}