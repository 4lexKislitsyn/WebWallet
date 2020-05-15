using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.Models
{
    /// <summary>
    /// Additional info about delete request.
    /// </summary>
    public class DeleteTransferRequest
    {
        /// <summary>
        /// Wallet identifier.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string WalletId { get; set; }
    }
}
