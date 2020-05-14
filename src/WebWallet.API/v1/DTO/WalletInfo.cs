using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.DTO
{
    /// <summary>
    /// Info about wallet.
    /// </summary>
    public class WalletInfo
    {
        /// <summary>
        /// Identifier of user wallet entity.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Balances of wallet.
        /// </summary>
        public virtual IEnumerable<BalanceInfo> Balances { get; set; }
    }
}
