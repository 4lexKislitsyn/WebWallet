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
        public async Task<IActionResult> CreateTransfer(TransferInfo transferInfo, [FromServices] ICurrencyRateService currencyRateService)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!_repository.DoesWalletExist(transferInfo.WalletId.ToString()))
            {
                return BadRequest($"Unknown wallet. Check {nameof(transferInfo.WalletId)} property.");
            }

            var isCurrencyTransfer = transferInfo.From.IsDefined() && transferInfo.To.IsDefined();
            var rate = isCurrencyTransfer ? await currencyRateService.GetCurrencyRate(transferInfo.From, transferInfo.To) : null;
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
            };

            if (!_repository.DoesWalletContainsCurrency(transfer.UserWalletId, transfer.ToCurrencyId))
            {
                _repository.AddEntity(new CurrencyBalance
                {
                    Balance = 0,
                    Currency = transfer.ToCurrencyId,
                    WalletId = transfer.UserWalletId
                });
            }

            _repository.AddEntity(transfer);
            await _repository.SaveAsync();

            return Created($"{Url.RouteUrl(ApiConstants.TransferRoute)}/{transfer.Id.Replace("-", "")}", transfer);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="transferConfirmation"></param>
        /// <param name="currencyRateService"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> ConfirmTransfer(Guid id, TransferConfirmation transferConfirmation, [FromServices] ICurrencyRateService currencyRateService)
        {
            var transfer = _repository.FindTransferWithCurrencies(id.ToString());

            if (transfer == null || transfer.State != TransferState.Active)
            {
                return NotFound();
            }

            if (transfer.FromCurrency.IsNull() && transfer.ToCurrency.IsNull())
            {
                return Problem("Sorry, transfer is inconsistent. Please, contact technical support.");
            }

            if (transfer.UserWalletId != transferConfirmation.WalletId)
            {
                return Forbid();
            }

            if (!transfer.FromCurrency.IsNull() && transfer.FromCurrency.Balance < transfer.Amount)
            {
                return StatusCode((int)HttpStatusCode.PaymentRequired, "Not enough money to confirm transfer.");
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
        public async Task<IActionResult> DeleteTransfer(Guid id, DeleteTransferRequest deleteTransfer)
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