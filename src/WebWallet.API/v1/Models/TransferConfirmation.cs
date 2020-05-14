using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.Models
{
    /// <summary>
    /// Model of transfer confirmation.
    /// </summary>
    public class TransferConfirmation
    {
        /// <summary>
        /// Transfer wallet identifier.
        /// </summary>
        [Required]
        public string WalletId { get; set; }
    }
}
