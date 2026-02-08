using Microsoft.AspNetCore.Mvc;
using KIFRIOSSE.ASTFRI.SDK;

namespace KIFRIOSSE.ASTFRI.Web.API.Controllers
{
    [ApiController]
    [Route("/")]
    [Tags("ASTFRI Controller")]
    public class AstfriController : ControllerBase
    {
        private readonly AstfriCLI _astfriCLI;

        public AstfriController(AstfriCLI astfriCli)
        {
            _astfriCLI = astfriCli;
        }

        /// <summary>
        /// Chceck the availability of the API, environment details and dependencies.
        /// </summary>
        /// <returns></returns>
        [HttpGet("healthcheck")]
        public IActionResult GetHealthcheck()
        {
            string astfriVersion = "Unavailable";
            try
            {
                astfriVersion = _astfriCLI.GetVersion();
            }
            catch
            {
                // Ignore error for healthcheck simple response, or report error
                astfriVersion = "Error accessing CLI";
            }

            return Ok(new
            {
                OSDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                IsLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux),
                UserName = Environment.UserName,
                MachineName = Environment.MachineName,
                ASTFRIVersion= astfriVersion
            });
        }
    }
}
