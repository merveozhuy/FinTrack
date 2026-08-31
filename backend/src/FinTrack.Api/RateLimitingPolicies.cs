namespace FinTrack.Api;

/// <summary>Named rate limiting policies applied to controllers via [EnableRateLimiting].</summary>
public static class RateLimitingPolicies
{
    public const string Auth = "auth";
    public const string Assistant = "assistant";
}
