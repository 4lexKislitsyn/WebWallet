using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.ExternalAPI
{
    /// <summary>
    /// Configuration to setup <see cref="ECBCurrencyRateService"/>.
    /// </summary>
    public class ECBCurrencyConfiguration
    {
        /// <summary>
        /// Host address of service.
        /// </summary>
        public string BaseUrl { get; set; }
        /// <summary>
        /// Path to service call.
        /// </summary>
        public string RatePath { get; set; }
    }
}
