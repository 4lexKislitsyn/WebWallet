using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.Models
{
    /// <summary>
    /// Error model that can be returned to user.
    /// </summary>
    public class ErrorModel
    {
        /// <summary>
        /// Create an instance of <see cref="ErrorModel"/>.
        /// </summary>
        /// <param name="errorMessage"></param>
        public ErrorModel(string errorMessage)
        {
            Message = errorMessage ?? throw new ArgumentNullException("You should provide error message.");
        }
        /// <summary>
        /// Error message.
        /// </summary>
        public string Message { get; protected set; }
    }
}
