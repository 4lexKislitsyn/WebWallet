using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// Create an instance of <see cref="TransferController"/>.
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="mapper"></param>
        public TransferController(IWebWalletRepository repository, IMapper mapper, ICurrencyRateService rateService)
        {
            _repository = repository;
            _mapper = mapper;
            _rateService = rateService;
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
                return NotFound(new ErrorModel($"Unknown wallet. Check {nameof(transferInfo.WalletId)} property."));
            }

            if (transferInfo.From.IsDefined())
            {
                var currencyBalance = _repository.FindCurrency(transferInfo.WalletId.ToString(), transferInfo.From);
                if (currencyBalance == null)
                {
                    return NotFound(new ErrorModel($"You don't have \"{transferInfo.From}\" balance."));
                }
                if (currencyBalance.Balance < transferInfo.Amount)
                {
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
            //TODO: add Automapper.
            var transfer = new MoneyTransfer()
            {
                FromCurrencyId = transferInfo.From,
                ToCurrencyId = transferInfo.To,
                UserWalletId = transferInfo.WalletId.ToString(),
                ActualCurrencyRate = (double?)rate,
                Amount = transferInfo.Amount,
            };
            _repository.AddEntity(transfer);

            if (transfer.ToCurrencyId.IsDefined() && !_repository.DoesWalletContainsCurrency(transfer.UserWalletId, transfer.ToCurrencyId))
            {
                _repository.AddEntity(new CurrencyBalance
                {
                    Balance = 0,
                    Currency = transfer.ToCurrencyId,
                    WalletId = transfer.UserWalletId
                });
            }
            await _repository.SaveAsync();
            return Created($"{Url.RouteUrl(ApiConstants.TransferRoute)}/{transfer.Id}", _mapper.Map<TransferInfo>(transfer));
        }

        /// <summary>
        /// Confirm transfer.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transferConfirmation"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> ConfirmTransfer(string id, TransferConfirmation transferConfirmation)
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

            if (transfer.UserWalletId != transferConfirmation.WalletId.ToString())
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
        /// <param name="deleteTransfer"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransfer(string id, DeleteTransferRequest deleteTransfer)
        {
            var transfer = _repository.FindTransfer(id.ToString());

            if (transfer == null || transfer.State == TransferState.Completed)
            {
                return NotFound();
            }

            if (transfer.UserWalletId != deleteTransfer.WalletId)
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
                return (null, rate);
            }
            catch (Exception ex)
            {
                //TODO: log error
                var unavailableResult = StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorModel("Currency rate temporary unavailable. Please, try again later."));
                return (unavailableResult, null);
            }
        }
    }
}