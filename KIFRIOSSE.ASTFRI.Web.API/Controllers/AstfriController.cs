using Microsoft.AspNetCore.Mvc;
using KIFRIOSSE.ASTFRI.SDK;
using System.Diagnostics;
using System.Text.Json;

namespace KIFRIOSSE.ASTFRI.Web.API.Controllers
{
    [ApiController]
    [Route("/")]
    [Tags("ASTFRI Controller")]
    public class AstfriController : ControllerBase
    {
        private readonly AstfriCLI _astfriCLI;
        private readonly ILogger<AstfriController> _logger;

        public AstfriController(AstfriCLI astfriCli, ILogger<AstfriController> logger)
        {
            _astfriCLI = astfriCli;
            _logger = logger;
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
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "PostTransform request received | RequestId: {RequestId} | InputLib: {InputLib} | OutputLib: {OutputLib} | OutputConfig: {OutputConfig} | InputTextLength: {InputTextLength}",
                requestId, request.InputLib, request.OutputLib, request.OutputConfig, request.InputText?.Length ?? 0);

            // Validate input library type
             if (!_astfriCLI.Config.InputLibs.Contains(request.InputLib))
            {
                _logger.LogWarning(
                    "PostTransform rejected - Unsupported InputLib | RequestId: {RequestId} | InputLib: {InputLib} | OutputLib: {OutputLib} | SupportedInputLibs: {SupportedInputLibs}",
                    requestId, request.InputLib, request.OutputLib, string.Join(", ", _astfriCLI.Config.InputLibs));
                return BadRequest(new { Error = $"Input library '{request.InputLib}' is not supported. Supported input libraries: {string.Join(", ", _astfriCLI.Config.InputLibs)}" });
            }

            // Validate input
            if (string.IsNullOrEmpty(request.InputText))
            {
                _logger.LogWarning(
                    "PostTransform rejected - InputText is null or empty | RequestId: {RequestId} | InputLib: {InputLib} | OutputLib: {OutputLib}",
                    requestId, request.InputLib, request.OutputLib);
                return BadRequest(new { Error = "InputText cannot be null or empty." });
            }

            // Validate output library type
            if (!_astfriCLI.Config.OutputLibs.Contains(request.OutputLib))
            {
                _logger.LogWarning(
                    "PostTransform rejected - Unsupported OutputLib | RequestId: {RequestId} | InputLib: {InputLib} | OutputLib: {OutputLib} | SupportedOutputLibs: {SupportedOutputLibs}",
                    requestId, request.InputLib, request.OutputLib, string.Join(", ", _astfriCLI.Config.OutputLibs));
                return BadRequest(new { Error = $"Output library '{request.OutputLib}' is not supported. Supported output libraries: {string.Join(", ", _astfriCLI.Config.OutputLibs)}" });
            }

            try
            {
                _logger.LogDebug(
                    "Starting transformation | RequestId: {RequestId} | InputLib: {InputLib} | OutputLib: {OutputLib} | OutputConfig: {OutputConfig} | InputPreview: {InputPreview}",
                    requestId, request.InputLib, request.OutputLib, request.OutputConfig,
                    request.InputText.Length > 100 ? request.InputText.Substring(0, 100) + "..." : request.InputText);

                string result = _astfriCLI.RunTranslation(request.InputLib, request.InputText, request.OutputLib, request.OutputConfig);

                stopwatch.Stop();
                _logger.LogInformation(
                    "PostTransform completed successfully | RequestId: {RequestId} | OutputLength: {OutputLength} | DurationMs: {DurationMs}",
                    requestId, result?.Length ?? 0, stopwatch.ElapsedMilliseconds);

                _logger.LogDebug(
                    "Transformation result preview | RequestId: {RequestId} | OutputPreview: {OutputPreview}",
                    requestId, result?.Length > 100 ? result.Substring(0, 100) + "..." : result);

                return Ok(new { Output = result });
            }
            catch (ArgumentException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning(
                    "PostTransform failed - ArgumentException | RequestId: {RequestId} | DurationMs: {DurationMs} | ErrorMessage: {ErrorMessage} | InputLib: {InputLib} | OutputLib: {OutputLib} | OutputConfig: {OutputConfig}",
                    requestId, stopwatch.ElapsedMilliseconds, ex.Message, request.InputLib, request.OutputLib, request.OutputConfig);

                return BadRequest(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "PostTransform failed - Unexpected exception | RequestId: {RequestId} | DurationMs: {DurationMs} | InputLib: {InputLib} | OutputLib: {OutputLib} | InputTextLength: {InputTextLength}",
                    requestId, stopwatch.ElapsedMilliseconds, request.InputLib, request.OutputLib, request.InputText?.Length ?? 0);

                return StatusCode(500, new { Error = "An error occurred while processing the transformation.", Details = ex.Message });
            }
        }

        public record TransformRequest(string InputLib, string InputText, string OutputLib, JsonElement? OutputConfig = null);
    }
}
