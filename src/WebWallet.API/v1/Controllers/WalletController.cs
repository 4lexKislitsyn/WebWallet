using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebWallet.DB;
using WebWallet.DB.Entities;

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
        private readonly IWebWalletRepository _repository;

        /// <summary>
        /// Create an instance of <see cref="WalletController"/>.
        /// </summary>
        /// <param name="repository"></param>
        public WalletController(IWebWalletRepository repository)
        {
            _repository = repository;
        }
        /// <summary>
        /// Create new wallet/user.
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(UserWallet), (int)HttpStatusCode.OK)]
        [HttpPost]
        public async Task<IActionResult> CreateWallet()
        {
            var wallet = new UserWallet();
            if (!_repository.AddEntity(wallet))
            {
                return Problem();
            }
            else
            {
                await _repository.SaveAsync();
            }
            var replacedId = wallet.Id.Replace("-", string.Empty);
            return Created($"{Url.RouteUrl("wallet - route")}/{replacedId}", wallet);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <response code="404">Wallet with passed identifier wasn't found.</response>
        /// <response code="200">The wallet was found and passed as response body.</response>
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        [ProducesResponseType(typeof(UserWallet), (int)HttpStatusCode.OK)]
        [HttpGet("{id}")]
        public IActionResult GetWalletInfo(Guid id)
        {
            var wallet = _repository.FindWalletWithCurrencies(id.ToString());
            return Ok(wallet);
        }
    }
}