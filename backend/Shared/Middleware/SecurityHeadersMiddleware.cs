using Microsoft.AspNetCore.Http;

namespace Shared.Middleware;

/// <summary>
/// Middleware that adds security headers to all HTTP responses.
/// Protects against XSS, clickjacking, MIME sniffing, and other common attacks.
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME sniffing
        // Browser won't try to guess content type if it's wrong
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options: Prevents clickjacking
        // Page cannot be embedded in iframe/frame
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // X-XSS-Protection: Enables browser's XSS filter (legacy, but doesn't hurt)
        // Most modern browsers use CSP instead
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Strict-Transport-Security (HSTS): Force HTTPS for 1 year
        // Tells browser to ONLY use HTTPS for this domain
        // Note: Only sent if current request is HTTPS
        if (context.Request.IsHttps)
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Content-Security-Policy (CSP): Restrict where resources can load from
        // This is a STRICT policy - adjust based on your needs
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +                           // Only load from same origin by default
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +  // Scripts (unsafe-inline/eval for React dev mode)
            "style-src 'self' 'unsafe-inline'; " +             // Styles (unsafe-inline for Tailwind)
            "img-src 'self' data: https:; " +                  // Images from self, data URIs, or HTTPS
            "font-src 'self' data:; " +                        // Fonts from self or data URIs
            "connect-src 'self'; " +                           // API calls only to same origin
            "frame-ancestors 'none'; " +                       // Can't be embedded (same as X-Frame-Options)
            "base-uri 'self'; " +                              // Restrict <base> tag
            "form-action 'self'";                              // Forms can only submit to same origin

        // Referrer-Policy: Control how much referrer information is sent
        // "strict-origin-when-cross-origin" = Send full URL for same-origin, only origin for cross-origin
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions-Policy: Control browser features (camera, microphone, geolocation, etc.)
        // Disable all features by default
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), " +           // No camera access
            "microphone=(), " +       // No microphone access
            "geolocation=(), " +      // No geolocation
            "payment=(), " +          // No payment API
            "usb=(), " +              // No USB access
            "magnetometer=(), " +     // No magnetometer
            "gyroscope=(), " +        // No gyroscope
            "accelerometer=()";       // No accelerometer

        // X-Permitted-Cross-Domain-Policies: Restrict Adobe Flash/PDF cross-domain access
        // (Legacy, but Adobe products still respect it)
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // Continue to next middleware
        await _next(context);
    }
}






