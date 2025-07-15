using Booklify.Application.Common.DTOs.Payment;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Queries.GetPaymentHistory;

/// <summary>
/// Query to get payment history for a user
/// </summary>
public class GetPaymentHistoryQuery : IRequest<Result<List<PaymentHistoryResponse>>>
{
    public Guid UserId { get; set; }
    
    public GetPaymentHistoryQuery(Guid userId)
    {
        UserId = userId;
    }
} 