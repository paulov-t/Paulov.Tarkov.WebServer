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

        public static string GetSessionId(HttpRequest requests)
        {
            IHeaderDictionary HttpHeaders = requests.Headers;
            if (HttpHeaders.ContainsKey("Cookie"))
            {
                Dictionary<string, string> HeaderCookie = new Dictionary<string, string>();
                var Cookie = HttpHeaders["Cookie"].ToString();
                foreach(var cookieSplitComma in Cookie.Split(',')) 
                { 
                    HeaderCookie.Add(Cookie.Split("=")[0], Cookie.Split("=")[1]);
                }

                if(HeaderCookie.ContainsKey("PHPSESSID"))
                    return HeaderCookie["PHPSESSID"];
            }

            return null;
        }
    }
}
