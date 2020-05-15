using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API
{
    /// <summary>
    /// Constants.
    /// </summary>
    public static class ApiConstants
    {
        /// <summary>
        /// Api version 1.0.
        /// </summary>
        public const string V1 = "1.0";
        /// <summary>
        /// Route name for transfer controller.
        /// </summary>
        public const string TransferRoute = "transfer-route";
        /// <summary>
        /// Route name for wallet controller.
        /// </summary>
        public const string WalletRoute = "wallet-route";
    }
}
