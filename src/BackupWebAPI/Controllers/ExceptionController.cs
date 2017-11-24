using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics;
using BackupWebAPI.Models;

namespace BackupWebAPI.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class ExceptionController : Controller
    {
        public IActionResult Index()
        {
            var ex = HttpContext.Features.Get<IExceptionHandlerFeature>();

            return StatusCode(StatusCodes.Status500InternalServerError, new ExceptionMessage { Message = ex.Error.Message });
        }

    }
}