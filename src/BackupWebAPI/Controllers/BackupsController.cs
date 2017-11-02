using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using BackupDatabase;
using BackupDatabase.Models;
using BackupWebAPI.Filters;
using Newtonsoft.Json;

namespace BackupWeb.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize]
    public class BackupsController : Controller
    {
        private readonly IMetaDBAccess metaDB;

        public BackupsController(IMetaDBAccess metaDB)
        {
            this.metaDB = metaDB;
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> Get(Guid id)
        {
            return Ok(await metaDB.GetBackup(id));
        }

        [HttpGet("[action]/{serverId:Guid}")]
        public async Task<IActionResult> ByServer(Guid serverId)
        {
            return Ok(await metaDB.GetBackups(serverId));
        }

        [HttpDelete("{id:Guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await metaDB.CancelBackup(id);
            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await metaDB.GetBackups());
        }

    }
}