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

        private async Task<DBServer> GetServer(Guid id, ServerType type)
        {
            DBServer server = null;
            switch (type)
            {
                case ServerType.Windows:
                    server = await metaDB.GetWindowsServer(id);
                    break;
                case ServerType.VMware:
                    server = await metaDB.GetVMWareServer(id);
                    break;
            }

            return server;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var x = HttpContext.User.Claims;
            return new ObjectResult(await metaDB.GetServers());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetServer(Guid id, bool refresh = false)
        {
            var servertype = await metaDB.GetServerType(id);
            var server = await GetServer(id, servertype);
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

            return new ObjectResult(server);
        }

        [HttpGet("Drives/{id}")]
        public async Task<IActionResult> GetDrives(Guid id)
        {
            return new ObjectResult(await agentClient.GetDrives(id));

        }

        [HttpGet("Content/{id}")]
        public async Task<IActionResult> GetContent(Guid id, string folder)
        {
            return new ObjectResult(await agentClient.GetContent(id, folder));
        }

        [HttpPost("Windows")]
        public async Task<IActionResult> AddWindowsServer([FromBody] DBWindowsServer server)
        {
            return new ObjectResult(await metaDB.AddServer(server));
        }

        [HttpPost("VMware")]
        public async Task<IActionResult> AddVMwareServer([FromBody] DBVMwareServer server)
        {
            return new ObjectResult(await metaDB.AddServer(server));
        }

        [HttpPost("{id}/BackupNow")]
        public async Task<IActionResult> BackupNow(Guid id, [FromBody] string[] items)
        {
            if (items == null || items.Length == 0)
            {
                return BadRequest();
            }

            var now = DateTime.UtcNow;

            await metaDB.AddCalendarEntry(new DBCalendarEntry { FirstRun = now, NextRun = now, Items = items, Periodicity = Periodicity.None, Enabled= true, Server = id });

            return new NoContentResult();
        }

    }
}
