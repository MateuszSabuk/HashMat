using HashMat.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HashMat.Pages
{
    public class JohnModel : PageModel
    {
        private readonly IHubContext<CommandHub> _hubContext;

        public JohnModel(IHubContext<CommandHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost]
        public async Task<IActionResult> OnPostRunJTR()
        {
            Console.WriteLine("AAA");
            string output = await RunJTR();
            return new NoContentResult();
        }

        public async Task<string> RunJTR()
        {
            Console.WriteLine("BBB");
            string johnCommand = "/opt/john/run/john";

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "bash",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"-c \"{johnCommand}\""
            };

            string output = "";
            using (Process process = new Process { StartInfo = psi })
            {
                process.OutputDataReceived += async (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        output += e.Data;
                        await _hubContext.Clients.All.SendAsync("ReceiveOutput", e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();

                process.WaitForExit();
            }
            return output;
        }
    }
}
