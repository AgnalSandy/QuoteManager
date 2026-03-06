namespace QuoteManager.Infrastructure.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Path: {Path}", context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "text/html";

            var errorId = Guid.NewGuid().ToString();
            
            _logger.LogError(exception, "Error ID: {ErrorId} - {Message}", errorId, exception.Message);

            if (_environment.IsDevelopment())
            {
                return context.Response.WriteAsync($@"
                    <html>
                    <body style='font-family: Arial, sans-serif; padding: 20px;'>
                        <h1 style='color: #d32f2f;'>⚠️ An Error Occurred</h1>
                        <p><strong>Error ID:</strong> {errorId}</p>
                        <p><strong>Message:</strong> {exception.Message}</p>
                        <p><strong>Type:</strong> {exception.GetType().Name}</p>
                        <details>
                            <summary>Stack Trace</summary>
                            <pre>{exception.StackTrace}</pre>
                        </details>
                        <p><a href='/'>← Return to Home</a></p>
                    </body>
                    </html>
                ");
            }
            else
            {
                return context.Response.WriteAsync($@"
                    <html>
                    <body style='font-family: Arial, sans-serif; padding: 20px; text-align: center;'>
                        <h1 style='color: #d32f2f;'>⚠️ Something Went Wrong</h1>
                        <p>We're sorry, but something went wrong. Our team has been notified.</p>
                        <p><strong>Error ID:</strong> {errorId}</p>
                        <p>Please try again or contact support if the problem persists.</p>
                        <p><a href='/' style='color: #1976d2;'>← Return to Home</a></p>
                    </body>
                    </html>
                ");
            }
        }
    }
}
