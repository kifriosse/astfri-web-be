using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace KIFRIOSSE.ASTFRI.SDK
{
    public class AstfriCLI
    {
        public Configuration Config { get; init; }

        public AstfriCLI(Configuration config)
        {
            Config = config;
        }

        /// <summary>
        /// spusti ASTFRI CLI transformacu dat
        /// </summary>
        /// <param name="inputLib">typ vstupnej kniznice</param>
        /// <param name="inputText">base64encoded zdrojovy kod na vstupe</param>
        /// <param name="outputLib">typ vystupnej kniznice</param>
        /// <param name="outputConfig">volitelna konfiguracia vystupu, napr. cielovy jazyk pre generovanie</param>
        /// <returns>
        ///     vystup transformacie ako string
        /// </returns>
        public string RunTranslation(string inputLib, string inputText, string outputLib, JsonElement? outputConfig = null)
        {
            // validate input and output library types against configuration
            if (!Config.InputLibs.Contains(inputLib))
            {
                throw new ArgumentException($"Input library '{inputLib}' is not supported. Supported input libraries: {string.Join(", ", Config.InputLibs)}");
            }
            if (!Config.OutputLibs.Contains(outputLib))
            {
                throw new ArgumentException($"Output library '{outputLib}' is not supported. Supported output libraries: {string.Join(", ", Config.OutputLibs)}");
            }

            // save inputText to a temporary file, because some CLI tools may not support large input via standard input
            string decodedInput = Encoding.UTF8.GetString(Convert.FromBase64String(inputText));
            string tempInputFile = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.{inputLib}");
            File.WriteAllText(tempInputFile, decodedInput);
            string? tempOutputConfigFile = null;

             // if outputConfig is provided, save it to a temporary file and pass the path as an argument
            if (outputConfig.HasValue)
            {
                tempOutputConfigFile = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.json");
                var tmpOutputConfigValue = outputConfig.Value.ToString();
                File.WriteAllText(tempOutputConfigFile, tmpOutputConfigValue);
            }

            // build arguments
            string arguments = $"--input {inputLib} --input-file {tempInputFile} --output {outputLib}";

            if (!string.IsNullOrEmpty(tempOutputConfigFile))
            {
                arguments += $" --output-config-file {tempOutputConfigFile}";
            }

            try
            {
                var result = RunProcess(arguments);

                if (result.code != 0)
                {
                    throw new Exception($"ASTFRI CLI error (code {result.code}). stdErr: {result.stdError}, stdOut: {result.stdOutput}");
                }

                return result.stdOutput;
            }
            finally
            {
                // clean up temporary file
                File.Delete(tempInputFile);
                if (!string.IsNullOrEmpty(tempOutputConfigFile))
                {
                    File.Delete(tempOutputConfigFile);
                }
            }

        }

        /// <summary>
        /// zisti verziu ASTFRI CLI, aby sme mohli overit kompatibilitu a informovat pouzivatela o podporovanych funkciach
        /// </summary>
        /// <returns></returns>
        public string GetVersion()
        {
            return RunProcess("--version").stdOutput;
        }

        protected (int code, string stdOutput, string? stdError) RunProcess(string arguments, string? standardInput = null)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Config.ExecutablePath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = !string.IsNullOrEmpty(standardInput)
            };

            int? exitCode = null;
            string? stdOutput = null;
            string? stdError = null;
            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process is not null)
                {
                    if (!string.IsNullOrEmpty(standardInput))
                    {
                        using (var writer = process.StandardInput)
                        {
                            writer.Write(standardInput);
                        }
                    }

                    stdOutput = process.StandardOutput.ReadToEnd();
                    stdError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    exitCode = process.ExitCode;
                }
            }

            return (exitCode ?? -1, stdOutput?.Trim() ?? string.Empty, stdError);
        }

        public class Configuration
        {
            public string ExecutablePath { get; init; } = "astfri";
            public string[] InputLibs { get; init; } = Array.Empty<string>();
            public string[] OutputLibs { get; init; } = Array.Empty<string>();
        }
    }
}
