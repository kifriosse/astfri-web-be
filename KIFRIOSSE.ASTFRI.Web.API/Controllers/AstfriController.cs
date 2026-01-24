using Microsoft.AspNetCore.Mvc;

namespace KIFRIOSSE.ASTFRI.Web.API.Controllers
{
    [ApiController]
    [Route("/")]
    [Tags("ASTFRI Controller")]
    public class AstfriController : ControllerBase
    {
        /// <summary>
        /// Chceck the availability of the API, environment details and dependencies.
        /// </summary>
        /// <returns></returns>
        [HttpGet("healthcheck")]
        public IActionResult Get()
        {
            return Ok(new
            {
                OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                IsLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux),
                UserName = Environment.UserName,
                MachineName = Environment.MachineName,
                ASTFRIService = "NOT_IMPLEMENTED"
            });
        }
    }
}
