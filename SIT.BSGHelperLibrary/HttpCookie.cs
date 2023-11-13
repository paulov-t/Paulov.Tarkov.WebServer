using Microsoft.AspNetCore.Http;

namespace SIT.WebServer.Middleware
{
    public class HttpCookie
    {
        public string GetSessionId(HttpRequest request)
        {
            return request.Cookies["PHPSESSID"].ToString();
        }
    }
}
