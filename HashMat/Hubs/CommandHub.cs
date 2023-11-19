using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace HashMat.Hubs
{
    public class CommandHub : Hub
    {

        public async Task SendOutput(string output)
        {
            await Clients.All.SendAsync("ReceiveOutput", output);
        }
    }
}
