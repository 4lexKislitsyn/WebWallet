using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebWallet.API.ModelValidation
{
    internal class ModelValidationFailedResult : ObjectResult
    {
        public ModelValidationFailedResult(ModelStateDictionary modelState) : base(new InvalidModelState(modelState))
        {
            StatusCode = StatusCodes.Status400BadRequest;
        }
    }
}