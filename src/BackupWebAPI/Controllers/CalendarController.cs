﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BackupWebAPI.Filters;
using BackupDatabase;
using BackupDatabase.Models;

namespace BackupWebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly IMetaDBAccess metaDB;

        public CalendarController(IMetaDBAccess metaDB)
        {
            this.metaDB = metaDB;
        }

        [HttpPost]
        [ValidateModel]
        public async Task<IActionResult> Post([FromBody] DBCalendarEntry calEntry)
        {
            calEntry.Id = await metaDB.AddCalendarEntry(calEntry);

            return Ok(calEntry);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await metaDB.GetCalendarEntries());
        }

        [HttpGet("{id:Guid}")]
        public async Task<IActionResult> GetCalendarEntry(Guid id)
        {
            return Ok(await metaDB.GetCalendarEntry(id));
        }


    }
}