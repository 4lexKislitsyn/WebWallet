using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using WebWallet.API.ExternalAPI.Interfaces;
using WebWallet.API.v1.DTO;
using WebWallet.API.v1.Models;
using WebWallet.DB;
using WebWallet.DB.Entities;

namespace WebWallet.API.v1.Controllers
{
    /// <summary>
    /// Controller is responsible for creating and executing money transferring.
    /// </summary>
    [Route("api/[controller]", Name = ApiConstants.TransferRoute)]
    [ApiController]
    [ApiVersion(ApiConstants.V1)]
    public class TransferController : ControllerBase
    {
        private readonly IWebWalletRepository _repository;
        private readonly IMapper _mapper;
        private readonly ICurrencyRateService _rateService;
        private readonly ILogger<TransferController> _logger;

        /// <summary>
        /// Create an instance of <see cref="TransferController"/>.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="mapper"></param>
        /// <param name="rateService"></param>
        public TransferController(IWebWalletRepository repository, IMapper mapper, ICurrencyRateService rateService, ILogger<TransferController> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _rateService = rateService;
            _logger = logger;
        }
        /// <summary>
        /// Create transfer object and return URL for confirmation.
        /// </summary>
        /// <param name="transferInfo"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateTransfer(CreateTransfer transferInfo)
        {
            if (!_repository.DoesWalletExist(transferInfo.WalletId.ToString()))
            {
                _logger.LogInformation("Wallet with id {WalletId} was not found.", transferInfo.WalletId);
                return NotFound(new ErrorModel($"Unknown wallet. Check {nameof(transferInfo.WalletId)} property."));
            }

            if (transferInfo.From.IsDefined())
            {
                var currencyBalance = _repository.FindCurrency(transferInfo.WalletId.ToString(), transferInfo.From);
                if (currencyBalance == null)
                {
                    _logger.LogInformation("Wallet with id {WalletId} doesn't have currency '{From}'", transferInfo.WalletId, transferInfo.From);
                    return NotFound(new ErrorModel($"You don't have \"{transferInfo.From}\" balance."));
                }
                if (currencyBalance.Balance < transferInfo.Amount)
                {
                    _logger.LogInformation("Trying to create transfer for wallet (WalletId) and currency '{From}' (balance={Balance}, transfer amount={Amount}).", transferInfo.WalletId, transferInfo.From, transferInfo.Amount, currencyBalance.Balance);
                    return StatusCode((int)HttpStatusCode.PaymentRequired, new ErrorModel($"You don't have enough money on \"{transferInfo.From}\" balance."));
                }
            }

            var isCurrencyTransfer = transferInfo.From.IsDefined() && transferInfo.To.IsDefined();
            decimal? rate = null;
            if (isCurrencyTransfer)
            {
                var (result, rateValue) = await TryGetRate(transferInfo.From, transferInfo.To);
                if (result != null)
                {
                    return result;
                }
                rate = rateValue;
            }
            if (isCurrencyTransfer && !rate.HasValue)
            {
                return BadRequest(new ErrorModel("Sorry. Cannot transfer this currency."));
            }

            var transfer = _mapper.Map<MoneyTransfer>(transferInfo);
            transfer.ActualCurrencyRate = (double?)rate;
            _repository.AddEntity(transfer);

            if (transfer.ToCurrencyId.IsDefined() && !_repository.DoesWalletContainsCurrency(transfer.WalletId, transfer.ToCurrencyId))
            {
                _repository.AddEntity(new CurrencyBalance
                {
                    Balance = 0,
                    Currency = transfer.ToCurrencyId,
                    WalletId = transfer.WalletId
                });
            }
            await _repository.SaveAsync();
            return Created($"{Url.RouteUrl(ApiConstants.TransferRoute)}/{transfer.Id}", _mapper.Map<TransferInfo>(transfer));
        }

        /// <summary>
        /// Get info about transfer.
        /// </summary>
        /// <returns></returns>
        [HttpGet("{id}")]
        public IActionResult GetTransfer(string id, [FromQuery] TransferActionRequest actionRequest)
        {
            var transfer = _repository.FindTransfer(id);
            if (transfer == null)
            {
                _logger.LogInformation("Transfer with id {id} in wallet {WalletId} was not found.", actionRequest.WalletId);
                return NotFound(new ErrorModel("Transfer was not found."));
            }
            if (transfer.WalletId != actionRequest.WalletId)
            {
                _logger.LogInformation("Try to get information about transfer {id} was prevented (passed wallet id {WalletId}).", actionRequest.WalletId);
                return Forbid();
            }
            return Ok(_mapper.Map<TransferInfo>(transfer));
        }
        /// <summary>
        /// Confirm transfer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="actionRequest"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> ConfirmTransfer(string id, TransferActionRequest actionRequest)
        {
            var transfer = _repository.FindTransferWithCurrencies(id.ToString());

            if (transfer == null || transfer.State != TransferState.Active)
            {
                return NotFound(new ErrorModel("Suitable transfer to complete was not found."));
            }

            if (transfer.FromCurrency.IsNull() && transfer.ToCurrency.IsNull())
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new ErrorModel("Sorry, transfer is inconsistent. Please, contact technical support."));
            }

            if (transfer.WalletId != actionRequest.WalletId.ToString())
            {
                return Forbid();
            }

            if (!transfer.FromCurrency.IsNull() && transfer.FromCurrency.Balance < transfer.Amount)
            {
                return StatusCode((int)HttpStatusCode.PaymentRequired, new ErrorModel("Not enough money to confirm transfer."));
            }

            if (!transfer.FromCurrency.IsNull() && !transfer.ToCurrency.IsNull())
            {
                var (result, rate) = await TryGetRate(transfer.FromCurrencyId, transfer.ToCurrencyId);
                if (result != null)
                {
                    return result;
                }
                if (!rate.HasValue)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorModel("Sorry, cannot transfer this currency anymore."));
                }
                transfer.FromCurrency.Balance -= transfer.Amount;
                transfer.ToCurrency.Balance = (double)((decimal)transfer.Amount * rate.Value);
                transfer.ActualCurrencyRate = (double)rate.Value;
            } 
            else if (transfer.ToCurrency.IsNull())
            {
                transfer.FromCurrency.Balance -= transfer.Amount;
            }
            else
            {
                transfer.ToCurrency.Balance += transfer.Amount;
            }
            transfer.State = TransferState.Completed;
            await _repository.SaveAsync();

            return Ok();
        }
        /// <summary>
        /// Delete active transfer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="actionRequest"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransfer(string id, TransferActionRequest actionRequest)
        {
            var transfer = _repository.FindTransfer(id.ToString());

            if (transfer == null || transfer.State == TransferState.Completed)
            {
                if (transfer != null)
                {
                    _logger.LogWarning("Trying to delete {State} transfer.", transfer.State);
                }
                return NotFound(new ErrorModel("Suitable transfer to delete was not found. You can delete only active transfers."));
            }

            if (transfer.WalletId != actionRequest.WalletId)
            {
                return Forbid();
            }

            if (transfer.State != TransferState.Deleted)
            {
                transfer.State = TransferState.Deleted;
                await _repository.SaveAsync();
            }

            return Ok();
        }

        private async Task<(ObjectResult result, decimal? rateValue)> TryGetRate(string fromCurrency, string toCurrency)
        {
            try
            {
                var rate = await _rateService.GetCurrencyRate(fromCurrency, toCurrency);
                if (!rate.HasValue)
                {
                    _logger.LogWarning("Cannot get currency rate ({fromCurrency} -> {toCurrency}).", fromCurrency, toCurrency);
                }
                return (null, rate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot get response from rate service ({fromCurrency} -> {toCurrency}).", fromCurrency, toCurrency);
                var unavailableResult = StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorModel("Currency rate temporary unavailable. Please, try again later."));
                return (unavailableResult, null);
            }
        }
    }
}