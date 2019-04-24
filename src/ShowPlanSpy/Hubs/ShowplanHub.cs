using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace ShowplanSpy.Hubs
{
    [UsedImplicitly]
    public class ShowplanHub: Hub<IShowplanClient>
    {
        public override async Task OnConnectedAsync()
        {
            Log.Logger.Verbose("New client connection {id}", Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {           
            if (exception != null)
            {
                Log.Logger.Verbose("Client {id} disconnected. {message}", Context.ConnectionId, exception.Message);
            }
            else
            {
                Log.Logger.Verbose("Client {id} disconnected.", Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }
    }

    public interface IShowplanClient
    {
        Task ReceivePlan(ShowplanMessage evt);
    }

    public class ShowplanMessage
    {
        public string Showplan { get; set; }
        public ulong Duration { get; set; }
        public int EstimatedRows { get; set; }
        public int EstimatedCost { get; set; }
        public string SqlStatement { get; set; }
        public string QueryPlanHashString { get; set; }
        public string QueryPlanHandle { get; set; }
        public DateTimeOffset OccuredAt { get; set; }
        public bool GoodPlan { get; set; }
    }
}
