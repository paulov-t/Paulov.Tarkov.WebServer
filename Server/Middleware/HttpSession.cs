namespace SIT.WebServer.Middleware
{
    public class HttpSession
    {
        public static string GetSessionId(Dictionary<string, string> HttpHeaders)
        {
            if (HttpHeaders.ContainsKey("Cookie"))
            {
                var Cookie = HttpHeaders["Cookie"];
                var SessionId = Cookie.Split("=")[1];
                return SessionId;
            }
            return "";
        }

        public static string GetSessionId(IHeaderDictionary HttpHeaders)
        {
            if (HttpHeaders.ContainsKey("Cookie"))
            {
                var Cookie = HttpHeaders["Cookie"].ToString();
                var SessionId = Cookie.Split("=")[1];
                return SessionId;
            }
            return "";
        }
    }
}
