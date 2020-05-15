using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace WebWallet.DB.Entities
{
    public class MoneyTransfer
    {
        /// <summary>
        /// Identifier of transfer.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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
        public double? ActualCurrencyRate { get; set; }
        /// <summary>
        /// Transfer wallet identifier.
        /// </summary>
        public string UserWalletId { get; set; }
        /// <summary>
        /// Is transfer completed by user.
        /// </summary>
        public TransferState State { get; set; }
        /// <summary>
        /// Currency from which transfer is made.
        /// </summary>
        /// <remarks>Composite foreign key is setting by Fluent API.</remarks>
        public virtual CurrencyBalance FromCurrency { get; set; }
        /// <summary>
        /// Currency to which transfer is made.
        /// </summary>
        /// <remarks>Composite foreign key is setting by Fluent API.</remarks>
        public virtual CurrencyBalance ToCurrency { get; set; }
        /// <summary>
        /// Transfer wallet.
        /// </summary>
        [ForeignKey(nameof(UserWalletId))]
        public virtual UserWallet Wallet { get; set; }
    }
}
