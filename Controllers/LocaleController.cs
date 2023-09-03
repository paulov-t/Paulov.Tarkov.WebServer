using Microsoft.AspNetCore.Mvc;
using SIT.WebServer.Middleware;
using SIT.WebServer.Providers;

namespace SIT.WebServer.Controllers
{
    public class LocaleController : ControllerBase
    {
        private SaveProvider saveProvider { get; } = new SaveProvider();

        [Route("client/menu/locale/{language}")]
        [HttpPost]
        public async void MenuLocale([FromRoute]string language, int? retry, bool? debug)
        {
            DatabaseProvider.TryLoadLocales(out var locales, out var localesDict, out var languages);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(localesDict["menu_en"]
                , Request, Response);
        }

        [Route("client/languages")]
        [HttpPost]
        public async void Languages(int? retry, bool? debug)
        {
            var requestBody = await HttpBodyConverters.DecompressRequestBodyToDictionary(Request);

            DatabaseProvider.TryLoadLocales(out var locales, out var localesDict, out var languages);

            await HttpBodyConverters.CompressIntoResponseBodyBSG(languages
                , Request, Response);

            //await HttpBodyConverters.CompressDictionaryIntoResponseBody(
            //    new Dictionary<string, object>() { { "utc_time", DateTime.UtcNow.Ticks } }
            //    , Response);

            //return new OkObjectResult(Response.Body);
        }
    }
}
