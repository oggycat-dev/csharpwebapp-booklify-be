using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Booklify.Application.Common.Behaviors;

/// <summary>
/// Pipeline behavior to monitor performance of requests
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<TRequest> _logger;
    
    public PerformanceBehavior(ILogger<TRequest> logger)
    {
        _timer = new Stopwatch();
        _logger = logger;
    }
    
    /// <summary>
    /// Handle the performance monitoring pipeline
    /// </summary>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        _timer.Start();
        
        var response = await next();
        
        _timer.Stop();
        
        var elapsedMilliseconds = _timer.ElapsedMilliseconds;
        
        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            
            _logger.LogWarning("Booklify Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}",
                requestName, elapsedMilliseconds, request);
        }
        
        return response;
    }
} 