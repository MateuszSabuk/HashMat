using HashMat.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using HashMat.Helpers;
using System.Text.RegularExpressions;
using System.Diagnostics.Metrics;
using System.Reflection.Emit;
using static HashMat.Helpers.ProcessExtensions;

namespace HashMat.Pages
{
    public class JohnModel : PageModel
    {
        public List<string> Algorithms { get; set; }
        public List<string> Wordlists { get; set; }
        private readonly IHubContext<CommandHub> _hubContext;
        public JohnModel(IHubContext<CommandHub> hubContext)
        {
            _hubContext = hubContext;
            Algorithms = John.GetAvailableAlgorithms();
            Wordlists = John.GetAvailableWordlists();
        }
        private string displayedOutput = "";
        private static Process johnProcess;
        private static bool processKilled = false;

        ~JohnModel()
        {
            // Kill the process before the exit
            if (johnProcess != null && !johnProcess.HasExited)
            {
                try
                {
                    johnProcess.Kill();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending signal to process: {ex.Message}");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> OnPostRunJTR(IFormFile hashListFile, IFormFile wordListFile, string input, string algorithm, string wordListOption, string inputOption, string selectedWordList, string selectedEncoding, string incremental)
        {
            Console.WriteLine("OnPostRunJTR");
            John john = new John(hashListFile, wordListFile, input, algorithm, wordListOption, inputOption, selectedWordList, selectedEncoding, incremental == "true");
            string output = await RunJTR(john);
            return new NoContentResult();
        }

        [HttpPost]
        public async Task<IActionResult> OnPostSignal(string signal)
        {
            await SendSignal(signal);
            return new NoContentResult();
        }

        private async Task SendSignal(string signal)
        {
            // Handle the incoming signal
            Console.WriteLine($"Received signal: {signal}");

            // Implement logic to handle the signal, e.g., communicate with the john process
            if (johnProcess != null && !johnProcess.HasExited)
            {
                try
                {
                    switch (signal)
                    {
                        case "kill":
                            SendLine("KILL signal sent to process", "info");
                            johnProcess.Kill();
                            break;
                        case "kill-all":
                            SendLine("KILL ALL signal sent to process", "info");
                            johnProcess.Kill();
                            processKilled = true;
                            break;
                        case "check":
                            SendLine("CHECK signal sent to process", "info");
                            johnProcess.ProcessSignals(ProcessExtensions.Signum.SIGUSR1);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending signal to process: {ex.Message}");
                }
            } else
            {
                SendLine("No process is running", "problem");
            }
        }

        public async Task<string> RunJTR(John john)
        {
            processKilled= false;
            string problem = john.ValidateInput();
            //Console.WriteLine("Problem: "+problem);
            if (problem.Length > 0)
            {
                SendLine(problem, "problem");
                return problem;
            }


            // await RunCommand("bash", "-c \"echo A && sleep 2 && echo B && sleep 2 && echo C\"", "ABC");
            // Modify the command based on the new inputs
            string johnCommand = john.CreateJohnCommand();

            string commandOutput = await RunCommand("/opt/john/run/john", johnCommand, "normal");

            //check for automatic and other formats
            if (johnCommand.Contains("--format=all") && displayedOutput.Contains("Use the \"--format="))
            {
                MatchCollection matchList = Regex.Matches(displayedOutput, @"(?<=(--format=))\S+?(?="")");
                List<string> detectedFormats = matchList.Cast<Match>().Select(match => match.Value).ToList();

                foreach (var detectedFormat in detectedFormats) // Replace detectedFormats with the actual variable containing detected formats
                {
                    string command = Regex.Replace(johnCommand, @"(?<=(--format=))\S+", detectedFormat);

                    if (processKilled) { break; }
                    SendLine($"Running john for {detectedFormat} format","info");
                    commandOutput = await RunCommand("/opt/john/run/john", command, "normal");

                    // Process the output or handle it as needed
                    if (displayedOutput.Contains("--show"))
                    {
                        johnCommand = Regex.Replace(johnCommand, @"--((single)|(wordlist\S+))", "");
                        johnCommand += " --show ";
                        johnCommand = Regex.Replace(johnCommand, @"(?<=(--format=))\S+", detectedFormat);
                        return await RunCommand("/opt/john/run/john", johnCommand, "show");
                    }
                }
            }
            if (displayedOutput.Contains("--show"))
            {
                johnCommand = Regex.Replace(johnCommand, @"--((incremental)|(single)|(wordlist\S+))", "");
                johnCommand += " --show ";
                return await RunCommand("/opt/john/run/john", johnCommand, "show");
            }

            return commandOutput;
        }

        private async Task<string> RunCommand(string command, string arguments, string label = "")
        {
            if (processKilled) { return ""; }
            if (johnProcess != null && !johnProcess.HasExited)
            {
                SendLine("Cant run tho jonh processes at the same time (At least yet)", "warning");
            }
            Console.WriteLine($"Running john {arguments}");

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = command,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = arguments
            };

            string output = "";
            try
            {
                johnProcess = new Process { StartInfo = psi };
                    // Start the process
                johnProcess.Start();

                // Read the standard output and standard error streams asynchronously
                Task<string> outputReader = ReadOutputAsync(johnProcess.StandardOutput, label);
                Task<string> errorReader = ReadOutputAsync(johnProcess.StandardError, label);

                // Wait for both output and error to be read
                await Task.WhenAll(outputReader, errorReader);

                // Send the output line by line to the SignalR hub
                string combinedOutput = outputReader.Result + errorReader.Result;
                using (StringReader stringReader = new StringReader(combinedOutput))
                {
                    string line;
                    while ((line = await stringReader.ReadLineAsync()) != null)
                    {
                        displayedOutput += label + line;
                        SendLine(line, label);
                    }
                }

                // Wait for the process to exit
                johnProcess.WaitForExit();
            } catch (Exception ex) { }
            return output;
        }

        private async Task<string> ReadOutputAsync(StreamReader reader, string label)
        {
            StringBuilder outputBuilder = new StringBuilder();
            char[] buffer = new char[4096];
            int bytesRead;

            while ((bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                outputBuilder.Append(buffer, 0, bytesRead);
                string output = outputBuilder.ToString();
                int lastNewLineIndex = output.LastIndexOf('\n');

                if (lastNewLineIndex >= 0)
                {
                    string linesToSend = output.Substring(0, lastNewLineIndex + 1);
                    outputBuilder.Remove(0, lastNewLineIndex + 1);
                    if(linesToSend.Length > 0 && linesToSend[linesToSend.Length - 1]== '\n') 
                    {
                        linesToSend = linesToSend.Remove(linesToSend.Length - 1, 1);
                    }
                    foreach (var line in linesToSend.Split("\n"))
                    {
                        displayedOutput += label + line;
                        if (line.Contains(" option to force loading these as that type instead"))
                        {
                            continue;
                        }
                        // Send the lines to the SignalR hub
                        SendLine(line, label);
                    }
                }
            }

            return outputBuilder.ToString();
        }

        private async void SendLine(string line, string label = "")
        {
            if (line.Length == 0) return;
            label = ChangeLabel(line, label);
            if (label.Length > 0) { label = $"<{label}>"; }
            line = ChangeLine(line);
            await _hubContext.Clients.All.SendAsync("ReceiveOutput", $"{label}{line}");
        }

        private string ChangeLine(string line)
        {
            if (line.Contains("Press Ctrl-C to abort, or send SIGUSR1 to john process for status"))
            {
                return "Check and kill signals can be sent to the john process";
            }
            if (line.Contains("Use the \"--show"))
            {
                return "Running the show function";
            }
            return line;
        }

        private string ChangeLabel(string line, string label)
        {
            if (line.Contains(", but the string is also recognized as "))
            {
                return "info";
            }
            if (Regex.IsMatch(line, @"\d+Kp/s \d+Kc/s \d+KC/s"))
            {
                return "status";
            }
            return label;
        }
    }
}