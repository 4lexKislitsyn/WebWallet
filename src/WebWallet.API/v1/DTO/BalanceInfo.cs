using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.DTO
{
    /// <summary>
    /// Info about currency balance.
    /// </summary>
    public class BalanceInfo
    {
        /// <summary>
        /// The currency of balance.
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Wallet balance by currency.
        /// </summary>
        public double Balance { get; set; }
    }
}
