using Booklify.Application.Common.Interfaces.Repositories;
using Booklify.Application.Common.DTOs.Subscription;
using Booklify.Domain.Entities;
using Booklify.Infrastructure.Persistence;
using Booklify.Infrastructure.Repositories.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Booklify.Infrastructure.Repositories;

public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(BooklifyDbContext context) : base(context)
    {
    }

    public async Task<Subscription?> GetSubscriptionByIdAsync(Guid id)
    {
        return await GetByIdAsync(id);
    }

    public async Task<(List<Subscription> Subscriptions, int TotalCount)> GetPagedSubscriptionsAsync(SubscriptionFilterModel filter)
    {
        // Build filter predicate
        Expression<Func<Subscription, bool>> predicate = BuildFilterPredicate(filter);
        
        // Determine sorting property
        var orderByExpression = GetOrderByExpression(filter.SortBy);

        // Get paged data from base repository
        // Default to descending for CreatedAt (newest first) unless specified
        bool isAscending = string.IsNullOrEmpty(filter.SortBy) ? false : filter.IsAscending;
        
        return await GetPagedAsync(
            predicate,
            orderByExpression,
            isAscending,
            filter.PageNumber,
            filter.PageSize
        );
    }
    
    private Expression<Func<Subscription, bool>> BuildFilterPredicate(SubscriptionFilterModel filter)
    {
        // Start with always true condition
        Expression<Func<Subscription, bool>> predicate = s => true;
        
        // Apply conditional filters
        if (!string.IsNullOrEmpty(filter.Name))
        {
            predicate = predicate.CombineAnd(s => s.Name.ToLower().Contains(filter.Name.ToLower()));
        }
        
        if (filter.MinPrice.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.Price >= filter.MinPrice.Value);
        }
        
        if (filter.MaxPrice.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.Price <= filter.MaxPrice.Value);
        }
        
        if (filter.MinDuration.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.Duration >= filter.MinDuration.Value);
        }
        
        if (filter.MaxDuration.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.Duration <= filter.MaxDuration.Value);
        }
        
        if (filter.Status.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.Status == filter.Status.Value);
        }
        // Note: Unlike the public API, admin can see all subscriptions regardless of status
        
        if (filter.IsPopular.HasValue)
        {
            predicate = predicate.CombineAnd(s => s.IsPopular == filter.IsPopular.Value);
        }
        
        // Global search across name and description
        if (!string.IsNullOrEmpty(filter.SearchKeyword))
        {
            predicate = predicate.CombineAnd(s => 
                s.Name.ToLower().Contains(filter.SearchKeyword.ToLower()) ||
                (s.Description != null && s.Description.ToLower().Contains(filter.SearchKeyword.ToLower())));
        }
        
        return predicate;
    }

    private Expression<Func<Subscription, object>> GetOrderByExpression(string? sortBy)
    {
        // Default sort by DisplayOrder then CreatedAt if not specified
        if (string.IsNullOrEmpty(sortBy))
        {
            return s => s.DisplayOrder;
        }
        
        // Return appropriate sorting expression based on property name
        return sortBy.ToLower() switch
        {
            "name" => s => s.Name,
            "price" => s => s.Price,
            "duration" => s => s.Duration,
            "createdat" => s => s.CreatedAt,
            "displayorder" => s => s.DisplayOrder,
            _ => s => s.CreatedAt // Default to DisplayOrder for unrecognized properties
        };
    }
} 