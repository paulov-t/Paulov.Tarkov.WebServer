using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SIT.Coop
{
    public class CoopController : Controller
    {
        public CoopController()
        {
        }

        [Route("/coop/connect")]
        [HttpPost]
        public async Task<IActionResult> CoopConnect(int? retry, bool? debug)
        {
            return new JsonResult("");
        }

        [Route("/coop/server/spawnPoint/{serverId}")]
        [HttpGet]
        public async Task<IActionResult> GetCoopServerSpawnPoint(int? retry, bool? debug, string serverId)
        {
            return new JsonResult("");
        }

        [Route("/coop/server/friendlyAI/{serverId}")]
        [HttpGet]
        public async Task<IActionResult> GetFriendlyAI(int? retry, bool? debug, string serverId)
        {
            return new JsonResult("");
        }

        [Route("/coop/server/getAllForLocation")]
        [HttpPost]
        public async Task<byte[]> GetAllServersForLocation(int? retry, bool? debug)
        {
            return null;
        }

        [Route("/coop/server/create")]
        [HttpPost]
        public async Task<byte[]> CreateServer(int? retry, bool? debug)
        {
            return null;
        }
    }
}