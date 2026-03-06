using System.Diagnostics;

namespace QuoteManager.Infrastructure.Middleware
{
    /// <summary>
    /// Logs request performance metrics
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

        public PerformanceMonitoringMiddleware(
            RequestDelegate next,
            ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                
                if (elapsedMs > 1000) // Log slow requests (over 1 second)
                {
                    _logger.LogWarning(
                        "Slow request: {Method} {Path} took {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        elapsedMs);
                }
                else
                {
                    _logger.LogDebug(
                        "Request: {Method} {Path} completed in {ElapsedMs}ms",
                        context.Request.Method,
                        context.Request.Path,
                        elapsedMs);
                }
            }
        }
    }
}
