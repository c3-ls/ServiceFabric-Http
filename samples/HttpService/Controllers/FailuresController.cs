using Microsoft.AspNet.Mvc;
using System;
using System.Threading.Tasks;

namespace HttpService.Controllers
{
    [Route("api/[Controller]")]
    public class FailuresController : Controller
    {
        [Route("Exception")]
        public string Exception()
        {
            throw new InvalidOperationException("this is an exception");
        }

        [Route("Delay")]
        public async Task<string> Delay(int seconds)
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            return $"Response after {seconds} seconds";
        }
    }
}