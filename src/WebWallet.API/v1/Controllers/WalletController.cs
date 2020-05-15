using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebWallet.API.v1.DTO;
using WebWallet.DB;
using WebWallet.DB.Entities;

namespace WebWallet.API.v1.Controllers
{
    /// <summary>
    /// Controller is responsible for creating and getting info about wallet.
    /// </summary>
    [Route("api/[controller]", Name = ApiConstants.WalletRoute)]
    [ApiController]
    [ApiVersion(ApiConstants.V1)]
    public class WalletController : ControllerBase
    {
        private readonly IWebWalletRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<WalletController> _logger;

        /// <summary>
        /// Create an instance of <see cref="WalletController"/>.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="mapper"></param>
        /// <param name="logger"></param>
        public WalletController(IWebWalletRepository repository, IMapper mapper, ILogger<WalletController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }
        /// <summary>
        /// Create new wallet/user.
        /// </summary>
        /// <returns></returns>
        [ProducesResponseType(typeof(UserWallet), (int)HttpStatusCode.OK)]
        [HttpPost]
        public async Task<IActionResult> CreateWallet()
        {
            _logger.LogTrace("Got create wallet request.");
            var wallet = new UserWallet();
            if (!_repository.AddEntity(wallet))
            {
                _logger.LogCritical("Cannot add wallet entity to repository.");
                return Problem();
            }
            else
            {
                await _repository.SaveAsync();
                _logger.LogInformation("New wallet was created (Id).", wallet.Id);
            }
            return Created($"{Url.RouteUrl(ApiConstants.WalletRoute)}/{wallet.Id}", _mapper.Map<WalletInfo>(wallet));
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
        public IActionResult GetWalletInfo(string id)
        {
            var wallet = _repository.FindWalletWithCurrencies(id.ToString());
            return wallet == null ? (IActionResult) NotFound() : Ok(_mapper.Map<WalletInfo>(wallet));
        }
    }
}