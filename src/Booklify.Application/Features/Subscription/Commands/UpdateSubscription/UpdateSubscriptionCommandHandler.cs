using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Commands.UpdateSubscription;

public class UpdateSubscriptionCommandHandler : IRequestHandler<UpdateSubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly IBooklifyDbContext _context;
    private readonly IMapper _mapper;

    public UpdateSubscriptionCommandHandler(IBooklifyDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<SubscriptionResponse>> Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Find subscription
        var subscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == request.Id && s.Status == EntityStatus.Active, cancellationToken);

        if (subscription == null)
        {
            return Result<SubscriptionResponse>.Failure("Subscription plan not found", ErrorCode.NotFound);
        }

        // Check for name conflict if name is being updated
        if (!string.IsNullOrEmpty(request.Request.Name) && 
            request.Request.Name.ToLower() != subscription.Name.ToLower())
        {
            var existingSubscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.Name.ToLower() == request.Request.Name.ToLower() && 
                                   s.Status == EntityStatus.Active && 
                                   s.Id != request.Id, cancellationToken);

            if (existingSubscription != null)
            {
                return Result<SubscriptionResponse>.Failure("Subscription with this name already exists", ErrorCode.DuplicateEntry);
            }
        }

        // Update subscription fields
        if (!string.IsNullOrEmpty(request.Request.Name))
            subscription.Name = request.Request.Name;

        if (request.Request.Description != null)
            subscription.Description = request.Request.Description;

        if (request.Request.Price.HasValue)
            subscription.Price = request.Request.Price.Value;

        if (request.Request.Duration.HasValue)
            subscription.Duration = request.Request.Duration.Value;

        if (request.Request.Features != null)
            subscription.Features = string.Join(";", request.Request.Features);

        if (request.Request.IsPopular.HasValue)
            subscription.IsPopular = request.Request.IsPopular.Value;

        if (request.Request.DisplayOrder.HasValue)
            subscription.DisplayOrder = request.Request.DisplayOrder.Value;

        if (request.Request.Status.HasValue)
            subscription.Status = request.Request.Status.Value;

        subscription.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<SubscriptionResponse>(subscription);
        return Result<SubscriptionResponse>.Success(response, "Subscription plan updated successfully");
    }
} 