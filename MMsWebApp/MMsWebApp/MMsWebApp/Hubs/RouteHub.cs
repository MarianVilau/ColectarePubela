using Microsoft.AspNetCore.SignalR;

namespace MMsWebApp.Hubs
{
    public class RouteHub : Hub
    {
        public async Task SendRouteUpdate(string message, double[] coordinates = null)
        {
            await Clients.All.SendAsync("ReceiveRouteUpdate", message, coordinates);
        }

        public async Task SendDistanceCheck(int from, int to, double distance)
        {
            await Clients.All.SendAsync("ReceiveDistanceCheck", from, to, distance);
        }
    }
} 