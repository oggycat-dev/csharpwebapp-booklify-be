using MediatR;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Application.Common.Interfaces;
using Booklify.Application.Common.Models;
using Booklify.Domain.Entities;
using Booklify.Domain.Enums;

namespace Booklify.Application.Features.Subscription.Commands.CreateSubscription;

public class CreateSubscriptionCommandHandler : IRequestHandler<CreateSubscriptionCommand, Result<SubscriptionResponse>>
{
    private readonly IBooklifyDbContext _context;
    private readonly IMapper _mapper;

    public CreateSubscriptionCommandHandler(IBooklifyDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<SubscriptionResponse>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        // Check if subscription name already exists
        var existingSubscription = await _context.Subscriptions
            .FirstOrDefaultAsync(s => s.Name.ToLower() == request.Request.Name.ToLower() && 
                               s.Status == EntityStatus.Active, cancellationToken);

        if (existingSubscription != null)
        {
            return Result<SubscriptionResponse>.Failure("Subscription with this name already exists", ErrorCode.DuplicateEntry);
        }

        // Create new subscription
        var subscription = new Domain.Entities.Subscription
        {
            Name = request.Request.Name,
            Description = request.Request.Description,
            Price = request.Request.Price,
            Duration = request.Request.Duration,
            Features = string.Join(";", request.Request.Features), // Store as semicolon-separated string
            IsPopular = request.Request.IsPopular,
            DisplayOrder = request.Request.DisplayOrder,
            Status = request.Request.Status,
            CreatedAt = DateTime.UtcNow
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<SubscriptionResponse>(subscription);
        return Result<SubscriptionResponse>.Success(response, "Subscription plan created successfully");
    }
} 