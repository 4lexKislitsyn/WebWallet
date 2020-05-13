using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.ExternalAPI.Interfaces
{
    /// <summary>
    /// Interface of service which can calculate currency rate.
    /// </summary>
    public interface ICurrencyRateService
    {
        /// <summary>
        /// Calculate currency rate.
        /// </summary>
        /// <param name="fromCurrency">Currency identifier from which transfer is executing.</param>
        /// <param name="toCurrency">Currency identifier to which transfer is executing.</param>
        /// <returns>The amount target currency of 1 unit of the source currency.</returns>
        Task<decimal?> GetCurrencyRate(string fromCurrency, string toCurrency);
    }
}
