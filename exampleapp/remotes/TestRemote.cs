using System.Threading.Tasks;
using NPitaya.Models;

namespace exampleapp.remotes
{
#pragma warning disable 1998
    class TestRemote : BaseRemote
    {
        public async Task<NPitaya.Protos.MyResponse> Remote(NPitaya.Protos.RPCMsg msg)
        {
            var response = new NPitaya.Protos.MyResponse
            {
                Msg = $"[REMOTE] hello from csharp :) {System.Guid.NewGuid().ToString()}, route={msg.Route}, msg={msg.Msg}",
                Code = 200,
            };
            Logger.Info($"Received RPC with message {msg.Msg}, {msg.Route}");
            return response;
        }
    }
}