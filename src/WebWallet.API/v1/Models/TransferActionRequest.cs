using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.Models
{
    /// <summary>
    /// Model for Delete/Put/Get requests.
    /// </summary>
    public class TransferActionRequest
    {
        /// <summary>
        /// Transfer wallet identifier.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string WalletId { get; set; }
    }
}
