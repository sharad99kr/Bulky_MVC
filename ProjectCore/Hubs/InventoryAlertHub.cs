using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjectCore.Hubs
{
    [Authorize(Roles ="Admin")]
    public class InventoryAlertHub : Hub
    {
    }
}
