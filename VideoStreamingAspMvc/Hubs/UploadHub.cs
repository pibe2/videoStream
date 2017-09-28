using Microsoft.AspNet.SignalR;

namespace VideoStreamingAspMvc.Hubs
{
    public class UploadHub : Hub
    {
        public void Hello()
        {
            Clients.All.hello();
        }

        public void Send(string name, string message)
        {
            Clients.All.addNewMessageToPage(name, message);
        }
    }
}