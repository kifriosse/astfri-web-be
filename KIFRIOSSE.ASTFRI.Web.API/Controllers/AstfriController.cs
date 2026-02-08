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

        /// <summary>
        /// Transforms the input code from one library format to another using the ASTFRI CLI.
        /// </summary>
        /// <param name="request">The transformation request containing input library, input text, and output library.</param>
        /// <returns>The transformed code or an error message.</returns>
        [HttpPost("transform")]
        public IActionResult PostTransform([FromBody] TransformRequest request)
        {
            try
            {
                string result = _astfriCLI.RunTranslation(request.InputLib, request.InputText, request.OutputLib);
                return Ok(new { Output = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "An error occurred while processing the transformation.", Details = ex.Message });
            }
        }

        public record TransformRequest(string InputLib, string InputText, string OutputLib);
    }
}
