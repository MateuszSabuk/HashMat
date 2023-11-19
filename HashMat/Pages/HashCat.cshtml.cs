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

namespace HashMat.Pages
{
    public class HashCatModel : PageModel
    {
        private readonly IHubContext<CommandHub> _hubContext;
        public HashCatModel(IHubContext<CommandHub> hubContext)
        {
            _hubContext = hubContext;
        }
    }
}