using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;

namespace PeerReview.Api.Controllers
{
    [ApiController]
    [Route("debug")]
    public class DebugController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public DebugController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("paths")]
        public IActionResult GetPaths()
        {
            var webRoot = _env.WebRootPath ?? "(null)";
            var contentRoot = _env.ContentRootPath ?? "(null)";

            var uploadsPhysical = webRoot == "(null)"
                ? "(no webroot)"
                : Path.Combine(webRoot, "uploads");

            string[] files = new string[0];

            if (Directory.Exists(uploadsPhysical))
            {
                files = Directory.GetFiles(uploadsPhysical)
                                 .Select(Path.GetFileName)
                                 .ToArray();
            }

            return Ok(new
            {
                ContentRoot = contentRoot,
                WebRoot = webRoot,
                UploadsPhysical = uploadsPhysical,
                FilesInUploads = files
            });
        }
    }
}
