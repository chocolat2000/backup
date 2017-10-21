using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupNetworkLibrary.Model
{
    public class NetCommand : INetworkMessage
    {
        public NetworkMessageTypes MsgType => NetworkMessageTypes.NetCommand;

        [JsonProperty("command")]
        public CommandType Type { get; set; }

        [JsonProperty("items")]
        public IEnumerable<string> Items { get; set; }

    }

    public enum CommandType
    {
        Backup
    }

}
