using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebWallet.API.Helpers.ModelValidation
{
    /// <summary>
    /// Attribute to validate requests content.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ValidateModelAtrribute : Attribute, IActionFilter
    {
        ///<inheritdoc/>
        public void OnActionExecuted(ActionExecutedContext context)
        {
        }
        ///<inheritdoc/>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                context.Result = new ModelValidationFailedResult(context.ModelState);
            }
        }
    }
}
