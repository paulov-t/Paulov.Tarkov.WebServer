namespace SIT.WebServer.Middleware
{
    public class HttpSessionHelpers
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

        public static string GetSessionId(HttpRequest request, HttpContext context = null)
        {
            IHeaderDictionary HttpHeaders = request.Headers;
            if (HttpHeaders.ContainsKey("Cookie"))
            {
                Dictionary<string, string> HeaderCookie = new Dictionary<string, string>();
                var Cookie = HttpHeaders["Cookie"].ToString();
                var cookieSplit = Cookie.Split(',');
                foreach (var cookieSplitComma in cookieSplit) 
                { 
                    if(!HeaderCookie.ContainsKey(cookieSplitComma.Split("=")[0]))
                        HeaderCookie.Add(cookieSplitComma.Split("=")[0], cookieSplitComma.Split("=")[1]);
                }

                if (context != null && context.Session != null && HeaderCookie.ContainsKey("PHPSESSID") )
                {
                    if(!context.Session.TryGetValue("SessionId", out _))
                        context.Session?.SetString("SessionId", HeaderCookie["PHPSESSID"]);
                }

                if(HeaderCookie.ContainsKey("PHPSESSID"))
                    return HeaderCookie["PHPSESSID"];
                else
                {

                }
            }

            if (context != null)
            {
                if(!context.Session.TryGetValue("SessionId", out _))
                    return context.Session?.GetString("SessionId");
            }

            return null;
        }
    }
}
