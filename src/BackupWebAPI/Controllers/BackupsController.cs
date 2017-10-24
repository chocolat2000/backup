using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BackupDatabase;

namespace BackupWeb.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class BackupsController : Controller
    {
        private readonly IMetaDBAccess metaDB;

        public BackupsController(IMetaDBAccess metaDB)
        {
            this.metaDB = metaDB;
        }

        [HttpGet("[action]/{serverId:Guid}")]
        public async Task<IActionResult> ByServer(Guid serverId)
        {
            return new ObjectResult(await metaDB.GetBackups(serverId));
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return new ObjectResult(await metaDB.GetBackups());
        }

    }
}