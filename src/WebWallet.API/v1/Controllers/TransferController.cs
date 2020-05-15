using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebWallet.API.ExternalAPI.Interfaces;
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
    [Helpers.ModelValidation.ValidateModelAtrribute]
    public class TransferController : ControllerBase
    {
        private readonly IWebWalletRepository _repository;

        /// <summary>
        /// Create an instance of <see cref="TransferController"/>.
        /// </summary>
        /// <param name="repository"></param>
        public TransferController(IWebWalletRepository repository)
        {
            _repository = repository;
        }
        /// <summary>
        /// Create transfer object and return URL for confirmation.
        /// </summary>
        /// <param name="transferInfo"></param>
        /// <param name="currencyRateService"></param>
        /// <returns></returns>
        [HttpPost]
        [Helpers.ModelValidation.ValidateModelAtrribute]
        public async Task<IActionResult> CreateTransfer(CreateTransfer transferInfo, [FromServices] ICurrencyRateService currencyRateService)
        {
            if (!_repository.DoesWalletExist(transferInfo.WalletId.ToString()))
            {
                return NotFound($"Unknown wallet. Check {nameof(transferInfo.WalletId)} property.");
            }

            if (transferInfo.From.IsDefined())
            {
                var currencyBalance = _repository.FindCurrency(transferInfo.WalletId.ToString(), transferInfo.From);
                if (currencyBalance == null)
                {
                    return NotFound($"You don't have \"{transferInfo.From}\" balance.");
                }
                if (currencyBalance.Balance < transferInfo.Amount)
                {
                    return StatusCode((int)HttpStatusCode.PaymentRequired, $"You don't have enough money on \"{transferInfo.From}\" balance.");
                }
            }

            var isCurrencyTransfer = transferInfo.From.IsDefined() && transferInfo.To.IsDefined();
            decimal? rate = null;
            if (isCurrencyTransfer)
            {
                try
                {
                    rate = await currencyRateService.GetCurrencyRate(transferInfo.From, transferInfo.To);
                }
                catch (Exception)
                {
                    return StatusCode(StatusCodes.Status503ServiceUnavailable, new ErrorModel("Currency rate temporary unavailable. Please, try again later."));
                }
            }
            if (isCurrencyTransfer && !rate.HasValue)
            {
                return BadRequest("Sorry. Cannot transfer this currency.");
            }
            //TODO: add Automapper.
            var transfer = new MoneyTransfer()
            {
                FromCurrencyId = transferInfo.From,
                ToCurrencyId = transferInfo.To,
                UserWalletId = transferInfo.WalletId.ToString(),
                ActualCurrencyRate = rate.HasValue ? (double)rate.Value : 0,
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
            return Created($"{Url.RouteUrl(ApiConstants.TransferRoute)}/{transfer.Id}", transfer);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transferConfirmation"></param>
        /// <param name="currencyRateService"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> ConfirmTransfer(string id, TransferConfirmation transferConfirmation, [FromServices] ICurrencyRateService currencyRateService)
        {
            var transfer = _repository.FindTransferWithCurrencies(id.ToString());

            if (transfer == null || transfer.State != TransferState.Active)
            {
                return NotFound();
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
                var rate = await currencyRateService.GetCurrencyRate(transfer.FromCurrencyId, transfer.ToCurrencyId);
                if (!rate.HasValue)
                {
                    return Problem("Sorry, cannot transfer this currency anymore.");
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
    }
}