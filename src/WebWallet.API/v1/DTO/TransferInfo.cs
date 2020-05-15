using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.DTO
{
    /// <summary>
    /// Info about transfer.
    /// </summary>
    public class TransferInfo
    {
        /// <summary>
        /// Identifier of transfer.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Currency identifier from which transfer is made.
        /// </summary>
        public string From { get; set; }
        /// <summary>
        /// Currency identifier to which transfer is made. Can be <see langword="null"/> if withdrawal is made. 
        /// </summary>
        public string To { get; set; }
        /// <summary>
        /// Transfer amount.
        /// </summary>
        public double Amount { get; set; }
        /// <summary>
        /// Currency rate on moment when transfer was made.
        /// </summary>
        public double? Rate { get; set; }
        /// <summary>
        /// Is transfer completed by user.
        /// </summary>
        public string State { get; set; }
    }
}
