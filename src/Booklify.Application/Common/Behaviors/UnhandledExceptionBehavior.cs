using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior to log unhandled exceptions
/// </summary>
public class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<TRequest> _logger;

    public UnhandledExceptionBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handle the exception logging pipeline
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;
            
            _logger.LogError(ex, "Booklify Request: Unhandled Exception for Request {Name} {@Request}", requestName, request);
            
            throw;
        }
    }
} 