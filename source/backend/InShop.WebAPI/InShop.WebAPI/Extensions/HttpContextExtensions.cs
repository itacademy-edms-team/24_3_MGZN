namespace InShop.WebAPI.Extensions
{
    public static class HttpContextExtensions
    {
        public static int? GetSessionId(this HttpContext context)
        {
            if (context.Items.TryGetValue("SessionId", out var value) && value is int sessionId)
            {
                return sessionId;
            }
            return null;
        }
    }
}
