using System.Runtime.InteropServices;

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
        /// <param name="inputText">zdrojovy kod na vstupe</param>
        /// <param name="outputLib">typ vystupnej kniznice</param>
        /// <returns>
        ///     vystup transformacie ako string
        /// </returns>
        public string RunTranslation(string inputLib, string inputText, string outputLib)
        {
            throw new NotImplementedException("This method is not implemented yet. It should run the ASTFRI CLI with the given input and output libraries and return the result as a string.");
        }

        /// <summary>
        /// zisti verziu ASTFRI CLI, aby sme mohli overit kompatibilitu a informovat pouzivatela o podporovanych funkciach
        /// </summary>
        /// <returns></returns>
        public string GetVersion()
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Config.ExecutablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            string astfriOutput = string.Empty;
            using (var process = System.Diagnostics.Process.Start(startInfo))
            {
                if (process is not null)
                {
                    astfriOutput = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
            }

            return astfriOutput.Trim();
        }

        public class Configuration
        {
            public string ExecutablePath { get; init; } = "astfri";
            public string[] InputLibs { get; init; } = Array.Empty<string>();
            public string[] OutputLibs { get; init; } = Array.Empty<string>();
        }
    }
}
