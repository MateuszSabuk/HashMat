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

namespace HashMat.Pages
{
    public class JohnModel : PageModel
    {
        public List<string> Algorithms { get; set; }
        private readonly IHubContext<CommandHub> _hubContext;
        public JohnModel(IHubContext<CommandHub> hubContext)
        {
            _hubContext = hubContext;
            Algorithms = John.GetAvailableAlgorithms();
        }
        private string displayedOutput = "";

        [HttpPost]
        public async Task<IActionResult> OnPostRunJTR(IFormFile hashListFile, IFormFile wordListFile, string input, string algorithm, string wordListOption, string inputOption, string selectedWordList)
        {
            John john = new John(hashListFile, wordListFile, input, algorithm, wordListOption, inputOption, selectedWordList);
            string output = await RunJTR(john);
            return new NoContentResult();
        }

        public async Task<string> RunJTR(John john)
        {
            string problem = john.ValidateInput();
            //Console.WriteLine("Problem: "+problem);
            if (problem.Length > 0)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveOutput", $"Problem:>> {problem} <<");
                return problem;
            }

            // Modify the command based on the new inputs
            string johnCommand = john.CreateJohnCommand();

            Console.WriteLine  ("JohnCommand: "+ johnCommand);

            string commandOutput = await RunCommand("/opt/john/run/john", johnCommand);
            if (displayedOutput.Contains("use \"--show\""))
            {
                if (Regex.IsMatch(johnCommand, @"--((single)|(wordlist\S+))"))
                {
                    johnCommand = Regex.Replace(johnCommand, @"--((single)|(wordlist\S+))", "--show");
                }
                else
                {
                    johnCommand += " --show ";
                }

                return await RunCommand("/opt/john/run/john", johnCommand);
            }
            return commandOutput;
        }

        private async Task<string> RunCommand(string command, string arguments)
        {
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
            using (Process process = new Process { StartInfo = psi })
            {
                // Start the process
                process.Start();

                // Read the standard output and standard error streams asynchronously
                Task<string> outputReader = ReadOutputAsync(process.StandardOutput);
                Task<string> errorReader = ReadOutputAsync(process.StandardError);

                // Wait for both output and error to be read
                await Task.WhenAll(outputReader, errorReader);

                // Send the output line by line to the SignalR hub
                string combinedOutput = outputReader.Result + errorReader.Result;
                using (StringReader stringReader = new StringReader(combinedOutput))
                {
                    string line;
                    while ((line = await stringReader.ReadLineAsync()) != null)
                    {
                        displayedOutput += line;
                        await _hubContext.Clients.All.SendAsync("ReceiveOutput", line);
                    }
                }

                // Wait for the process to exit
                process.WaitForExit();
            }
            return output;
        }

        private async Task<string> ReadOutputAsync(StreamReader reader)
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
                        displayedOutput += line;

                        // Send the lines to the SignalR hub
                        await _hubContext.Clients.All.SendAsync("ReceiveOutput", line);
                    }
                }
            }

            return outputBuilder.ToString();
        }
    }
}