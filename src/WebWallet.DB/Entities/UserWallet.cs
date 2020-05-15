using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace WebWallet.DB.Entities
{
    public class UserWallet
    {
        /// <summary>
        /// Identifier of user wallet entity.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        /// <summary>
        /// Balances of wallet.
        /// </summary>
        public virtual IEnumerable<CurrencyBalance> Balances { get; set; }
        /// <summary>
        /// Wallet transfers.
        /// </summary>
        public virtual IEnumerable<MoneyTransfer> Transfer { get; set; }
    }
}
