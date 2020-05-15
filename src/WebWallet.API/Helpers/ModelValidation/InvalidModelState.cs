using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace WebWallet.API.Helpers.ModelValidation
{
    internal class InvalidModelState
    {
        public string Message { get; set; } = "Model validation failed.";
        public ICollection<FieldValidationError> Errors { get; }

        public InvalidModelState(ModelStateDictionary modelState)
        {
            Errors = modelState.SelectMany(x => x.Value.Errors.Select(error => new FieldValidationError(x.Key, error.ErrorMessage))).ToList();
        }
    }

    internal class FieldValidationError
    {
        public FieldValidationError(string field, string message)
        {
            if (field.IsDefined())
            {
                Field = field;
            }
            Message = message;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Field { get; }

        public string Message { get; }
    }
}