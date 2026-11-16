using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TokenChat.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        public async Task SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            if (message.Length > 1000)
            {
                throw new HubException("Сообщение не должно превышать 1000 символов.");
            }

            // Имя берётся из проверенного JWT, а не из сообщения клиента.
            string username = Context.User.Identity.Name;
            await Clients.All.SendAsync("ReceiveMessage", username, message.Trim());
        }
    }
}
