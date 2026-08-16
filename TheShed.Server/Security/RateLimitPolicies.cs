namespace TheShed.Server.Security
{
    /// <summary>
    /// Names of the rate limiting policies, shared between the pipeline setup and the controllers
    /// so a typo cannot silently leave an endpoint unthrottled.
    /// </summary>
    public static class RateLimitPolicies
    {
        /// <summary>Throttles login and register per client IP.</summary>
        public const string Auth = "auth";
    }
}
