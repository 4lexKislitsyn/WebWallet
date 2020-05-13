using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebWallet.API.v1.Controllers
{
    /// <summary>
    /// Controller is responsible for creating and getting info about wallet.
    /// </summary>
    [Route("api/[controller]", Name = "wallet-route")]
    [ApiController]
    [ApiVersion(ApiConstants.V1)]
    public class WalletController : ControllerBase
    {
        /// <summary>
        /// Create new wallet/user.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult CreateWallet()
        {
            // TODO: create wallet
            var guid = Guid.Empty.ToString("N");

            return Created(Url.RouteUrl("wallet-route", guid), new { id = guid });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="404">Wallet with passed identifier wasn't found.</response>
        /// <response code="200">The wallet was found and passed as response body.</response>
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        [HttpGet("{id}")]
        public IActionResult GetWalletInfo(string id)
        {
            return Ok(new { id });
        }
    }
}