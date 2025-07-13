using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Models;
using MediatR;

namespace Booklify.Application.Features.User.Queries.GetSubscriptionHistory;

/// <summary>
/// Query to get subscription history for a user
/// </summary>
public class GetSubscriptionHistoryQuery : IRequest<Result<List<UserSubscriptionHistoryResponse>>>
{
    public Guid UserId { get; set; }
    
    public GetSubscriptionHistoryQuery(Guid userId)
    {
        UserId = userId;
    }
} 