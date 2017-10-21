using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupNetworkLibrary.Model
{
    public class ErrorMessage : INetworkMessage
    {
        public NetworkMessageTypes MsgType => NetworkMessageTypes.Error;

        [JsonProperty("message")]
        public string Message { get; set; }

    }
}
