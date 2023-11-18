using HashMat.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using HashMat.Helpers;


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
            if (problem.Length > 0)
            {
                return problem;
            }


            // Modify the command based on the new inputs
            string johnCommand = john.CreateJohnCommand();

            // You can now use the provided input values in the command construction

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "/opt/john/run/john",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = johnCommand
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

                    // Send the lines to the SignalR hub
                    await _hubContext.Clients.All.SendAsync("ReceiveOutput", linesToSend);
                }
            }

            return outputBuilder.ToString();
        }
    }
}