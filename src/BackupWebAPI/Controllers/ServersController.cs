﻿using System;
using System.Net;
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
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using BackupWebAPI.Models;

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

        private async Task<DBServer> GetServer(Guid id, bool withcreds = false)
        {
            var servertype = await metaDB.GetServerType(id);
            DBServer server = null;
            switch (servertype)
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
            
            var server = await GetServer(id, refresh);

            if (server == null) return NotFound();
            if (refresh)
            {
                switch (server.Type)
                {

                    case ServerType.VMware:

                        using (var proxy = new Vim25Proxy.Proxy(server.Ip))
                        {
                            var vmserver = server as DBVMwareServer;
                            await proxy.Login(vmserver.Username, vmserver.Password);
                            vmserver.Username = null;
                            vmserver.Password = null;
                            vmserver.VMs = (await proxy.GetVMs()).OrderBy(kv => kv.Name).Select(vm => new string[] { vm.MoRef, vm.Name }).ToArray();
                            await metaDB.UpdateServer(vmserver);
                        }
                        break;
                }
            }

            return Ok(server);
        }

        [HttpGet("{id:Guid}/arbo")]
        public async Task<IActionResult> GetArbo(Guid id)
        {
            var server = await GetServer(id, true);
            if (server == null) return NotFound();
            if (server.Type != ServerType.VMware)
                throw new ArgumentException($"Server GUID {{{id}}} is not of type {ServerType.VMware}");

            var arbo = new VMwareArbo();
            using (var proxy = new Vim25Proxy.Proxy(server.Ip))
            {
                var vmserver = server as DBVMwareServer;
                await proxy.Login(vmserver.Username, vmserver.Password);
                arbo.Folders = await proxy.GetFolders();
                arbo.Pools = await proxy.GetPools();
            }

            return Ok(arbo);
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
            var request = WebRequest.CreateHttp($"https://{server.Ip}");
            request.ServerCertificateValidationCallback += 
                (sender, certificate, chain, sslPolicyErrors) => 
                {
                    var cert2 = new X509Certificate2(certificate);
                    server.ThumbPrint = string.Join(':', Regex.Matches(cert2.Thumbprint, "..").Select(m => m.Value));
                    return true;
                };
            (await request.GetResponseAsync()).Close();

            return Ok(await metaDB.AddServer(server));
        }

        [HttpPut("{id:Guid}")]
        [Authorize(Roles = "admin")]
        [ValidateModel]
        public async Task<IActionResult> UpdateServer(Guid id, [FromBody] JToken server)
        {
            DBServer dBServer;
            switch (await metaDB.GetServerType(id))
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

            if (dBServer == null)
            {
                return NotFound();
            }
            await metaDB.UpdateServer(dBServer);
            return Ok(dBServer);
        }

    }
}
