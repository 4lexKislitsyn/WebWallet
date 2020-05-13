using System;
using System.Collections.Generic;
using System.Text;

namespace WebWallet.DB.Entities
{
    public class MoneyTransfer
    {
        /// <summary>
        /// Identifier of transfer.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Currency identifier from which transfer is made.
        /// </summary>
        public string FromCurrencyId { get; set; }
        /// <summary>
        /// Currency identifier to which transfer is made. Can be <see langword="null"/> if withdrawal is made. 
        /// </summary>
        public string ToCurrencyId { get; set; }
        /// <summary>
        /// Transfer amount.
        /// </summary>
        public double Amount { get; set; }
        /// <summary>
        /// Currency rate on moment when transfer was made.
        /// </summary>
        public double ActualCurrencyRate { get; set; }
        /// <summary>
        /// Transfer wallet identifier.
        /// </summary>
        public string UserWalletId { get; set; }
        /// <summary>
        /// Currency from which transfer is made.
        /// </summary>
        public virtual CurrencyBalance FromCurrency { get; set; }
        /// <summary>
        /// Currency to which transfer is made.
        /// </summary>
        public virtual CurrencyBalance ToCurrency { get; set; }
        /// <summary>
        /// Transfer wallet.
        /// </summary>
        public virtual UserWallet Wallet { get; set; }
    }
}
