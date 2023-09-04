using ComponentAce.Compression.Libs.zlib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Server.HttpSys;
using Newtonsoft.Json;
using SIT.WebServer.Middleware;
using SIT.WebServer.Providers;
using System.IO.Compression;
using System.Text;

namespace SIT.WebServer.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class LauncherController : ControllerBase
    {
        private SaveProvider saveProvider { get; } = new SaveProvider();

        /// <summary>
        /// Login to the Server
        /// </summary>
        /// <returns></returns>
        //[HttpPost(Name = "login")]
        [Route("launcher/profile/login", Name = "LauncherLogin")]
        [Route("launcher/profile/login/{username}", Name = "LauncherLoginWithUsername")]
        [HttpPost]
        public async void Login()
        {
            
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            var resolvedUserName = "";
            if (requestBody != null && requestBody.ContainsKey("username"))
            {
                resolvedUserName = requestBody["username"].ToString();
            }
            else if (Request.RouteValues.ContainsKey("username"))
            {
                resolvedUserName = Request.RouteValues["username"].ToString();
            }
            if (string.IsNullOrEmpty(resolvedUserName))
            {
                Response.StatusCode = 405; // unauthorized
                return;
            }

            if (saveProvider.ProfileExists(resolvedUserName, out var sessionId))
            {
                HttpContext.Session.Set("SessionId", Encoding.UTF8.GetBytes(sessionId));
                saveProvider.LoadProfile(sessionId);
                await HttpBodyConverters.CompressStringIntoResponseBody(sessionId, Request, Response);
            }
            else
                await HttpBodyConverters.CompressStringIntoResponseBody("FAILED", Request, Response);
        }

        /// <summary>
        /// Register to Server
        /// </summary>
        /// <returns></returns>
        //[HttpPost(Name = "login")]
        [Route("launcher/profile/register", Name = "LauncherRegister")]
        [HttpPost]
        public async void Register()
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);
            var sessionId = saveProvider.CreateAccount(requestBody);
            if (sessionId == null) 
                return;
            await HttpBodyConverters.CompressStringIntoResponseBody(sessionId, Request, Response);
        }
    }
}
