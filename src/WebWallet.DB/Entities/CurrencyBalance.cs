using System.ComponentModel.DataAnnotations.Schema;

namespace WebWallet.DB.Entities
{
    public class CurrencyBalance
    {
        /// <summary>
        /// The currency of balance.
        /// </summary>
        public string Currency { get; set; }

        /// <summary>
        /// Wallet balance by currency.
        /// </summary>
        public double Balance { get; set; }

        /// <summary>
        /// The identifier of the wallet to which balance belongs.
        /// </summary>
        public string WalletId { get; set; }

        /// <summary>
        /// The wallet to which balance belongs.
        /// </summary>
        [ForeignKey(nameof(WalletId))]
        public virtual UserWallet Wallet { get; set; }
    }
}