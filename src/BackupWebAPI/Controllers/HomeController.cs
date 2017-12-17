using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace BackupWebAPI.Controllers
{
    public class HomeController : Controller
    {
        public IHostingEnvironment HostingEnv { get; }

        public HomeController(IHostingEnvironment env)
        {
            HostingEnv = env;
        }

        [HttpGet]
        public IActionResult RedirectIndex()
        {
            return new PhysicalFileResult(
                Path.Combine(HostingEnv.WebRootPath, "index.html"),
                "text/html"
            );
        }

    }
}