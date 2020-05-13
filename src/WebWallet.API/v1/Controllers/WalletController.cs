using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebWallet.API.v1.Controllers
{
    [Route("api/[controller]", Name = "wallet-route")]
    [ApiController]
    public class WalletController : ControllerBase
    {
        [HttpPost]
        public IActionResult CreateWallet()
        {
            // TODO: create wallet
            var guid = Guid.Empty.ToString("N");

            return Created(Url.RouteUrl("wallet-route", guid), new { id = guid });
        }

        [HttpGet("{id}")]
        public IActionResult GetWalletInfo(string id)
        {
            return Ok(new { id });
        }
    }
}