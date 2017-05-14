using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace HttpService.Controllers
{
    [Route("api/[controller]")]
    public class FailuresController : Controller
    {
        [Route("Exception")]
        [HttpGet, HttpPost]
        public string Exception()
        {
            throw new InvalidOperationException("this is an exception");
        }

        [Route("Delay")]
        [HttpGet, HttpPost]
        public async Task<string> Delay(int seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            return $"Response after {seconds} seconds";
        }

        [Route("NotFoundWithHeader")]
        [HttpGet]
        public IActionResult NotFoundWithHeader()
        {
            Response.Headers["X-ServiceFabric"] = "ResourceNotFound";

            return NotFound();
        }
    }
}