using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BackupDatabase;
using BackupDatabase.Models;
using BackupWeb.Services;
using Microsoft.AspNetCore.Authorization;
using BackupWebAPI.Filters;
using Newtonsoft.Json.Linq;

namespace BackupWeb.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize]
    public class ServersController : Controller
    {
        private readonly AgentClient agentClient;
        private readonly IMetaDBAccess metaDB;

        public ServersController(IMetaDBAccess metaDB, AgentClient agentClient)
        {
            this.metaDB = metaDB;
            this.agentClient = agentClient;
        }

        private async Task<DBServer> GetServer(Guid id, ServerType type, bool withcreds = false)
        {
            DBServer server = null;
            switch (type)
            {
                case ServerType.Windows:
                    server = await metaDB.GetWindowsServer(id, withcreds);
                    break;
                case ServerType.VMware:
                    server = await metaDB.GetVMWareServer(id, withcreds);
                    break;
            }

            return server;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await metaDB.GetServers());
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(Guid id, bool refresh = false)
        {
            var servertype = await metaDB.GetServerType(id);
            DBServer server = null;
            try
            {
                server = await GetServer(id, servertype, refresh);
            }
            catch(Exception)
            {
                //Todo: some logging ...
            }
            if (server == null) return NotFound();
            if (refresh)
            {
                switch (servertype)
                {

                    case ServerType.VMware:

                        using (var proxy = new Vim25Proxy.Proxy(server.Ip))
                        {
                            var vmserver = server as DBVMwareServer;
                            await proxy.Login(vmserver.Username, vmserver.Password);
                            vmserver.VMs = await proxy.GetVMs();
                            await metaDB.AddServer(vmserver);
                        }
                        break;
                }
            }

            return Ok(server);
        }

        [HttpDelete("{id:Guid}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await metaDB.DeleteServer(id);
            return NoContent();
        }

        [HttpGet("{id:Guid}/drives")]
        public async Task<IActionResult> GetDrives(Guid id)
        {
            return Ok(await agentClient.GetDrives(id));

        }

        [HttpGet("{id:Guid}/content")]
        public async Task<IActionResult> GetContent(Guid id, string folder)
        {
            return Ok(await agentClient.GetContent(id, folder));
        }

        [HttpPost("windows")]
        [Authorize(Roles = "admin")]
        [ValidateModel]
        public async Task<IActionResult> AddWindowsServer([FromBody] DBWindowsServer server)
        {
            return Ok(await metaDB.AddServer(server));
        }

        [HttpPost("vmware")]
        [Authorize(Roles = "admin")]
        [ValidateModel]
        public async Task<IActionResult> AddVMwareServer([FromBody] DBVMwareServer server)
        {
            return Ok(await metaDB.AddServer(server));
        }

        [HttpPut("{id:Guid}")]
        [Authorize(Roles = "admin")]
        [ValidateModel]
        public async Task<IActionResult> UpdateServer(Guid id, [FromBody] JToken server)
        {
            DBServer dBServer;
            switch(await metaDB.GetServerType(id))
            {
                case ServerType.Windows:
                    {
                        dBServer = server.ToObject<DBWindowsServer>();
                    }
                    break;
                case ServerType.VMware:
                    {
                        dBServer = server.ToObject<DBVMwareServer>();
                    }
                    break;
                case ServerType.Undefined:
                default:
                    {
                        dBServer = null;
                    }
                    break;
            }

            if(dBServer == null)
            {
                return NotFound();
            }
            else
            {
                await metaDB.UpdateServer(dBServer);
            }
            return Ok();
        }

    }
}
