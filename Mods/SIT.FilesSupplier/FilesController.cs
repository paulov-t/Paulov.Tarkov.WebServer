using Microsoft.AspNetCore.Mvc;
using System.Runtime.Serialization;

namespace SIT.FilesSupplier
{
    public class FilesController : Controller
    { 
        public FilesController()
        {

        }

        [Route("/files/trader/avatar/{traderId}")]
        [HttpGet]
        public async Task<IActionResult> TraderAvatar(int? retry, bool? debug, string traderId)
        {
            Byte[] b = await System.IO.File.ReadAllBytesAsync(Path.Combine(AppContext.BaseDirectory, "assets", "images", "traders", traderId.Replace(".jpg", ".png")));   // You can use your own method over here.         
            return File(b, "image/png");
            //await HttpBodyConverters.CompressIntoResponseBodyBSG(JsonConvert.SerializeObject(queueData), Request, Response);
        }
    }
}