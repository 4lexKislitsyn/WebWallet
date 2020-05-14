using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.v1.Models
{
    /// <summary>
    /// Info about creating transfer.
    /// </summary>
    public class CreateTransfer : IValidatableObject
    {
        /// <summary>
        /// Currency identifier from transfer is making. Can be <see langword="null"/> if replenishment is executing.
        /// </summary>
        public string From { get; set; }
        /// <summary>
        /// Currency to which transfer is making. Can be <see langword="null"/> if withdrawal is executing.
        /// </summary>
        public string To { get; set; }
        /// <summary>
        /// Amount of money to transfer.
        /// </summary>
        [Required]
        public double Amount { get; set; }
        /// <summary>
        /// Wallet identifier.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string WalletId { get; set; }

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!From.IsDefined() && !To.IsDefined())
            {
                yield return new ValidationResult("At least one currency should be passed.", new[] { nameof(From), nameof(To) });
            }
        }
    }
}
