using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Get inverted value of function <see cref="String.IsNullOrWhiteSpace(string?)"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsDefined(this string value) => !string.IsNullOrWhiteSpace(value);
        /// <summary>
        /// Check is object <see langword="null"/>.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNull(this object value) => value == null;
    }
}
