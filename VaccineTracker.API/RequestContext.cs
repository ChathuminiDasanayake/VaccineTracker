using VaccineTracker.Application.Interfaces;

namespace VaccineTracker.API;

public sealed class RequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private HttpContext? HttpContext =>
        _httpContextAccessor.HttpContext;

    public string CorrelationId =>
        HttpContext?.TraceIdentifier ?? string.Empty;

    public string? IpAddress =>
        HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        HttpContext?.Request.Headers.UserAgent.FirstOrDefault();
}