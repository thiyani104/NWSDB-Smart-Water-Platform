using System.Security.Claims;

namespace NWSDB.API.Data.Services;

/// <summary>
/// Object-level authorization: decides whether the current principal may touch a given
/// water account, bill or payment. Staff and Admin may access everything; customers may
/// only access resources that belong to a connection they own.
/// </summary>
public interface IAccountAccessService
{
    Task<bool> CanAccessAccountAsync(ClaimsPrincipal user, string accountNumber);
    Task<bool> CanAccessBillAsync(ClaimsPrincipal user, int billId);
    Task<bool> CanAccessPaymentAsync(ClaimsPrincipal user, int paymentId);
}
