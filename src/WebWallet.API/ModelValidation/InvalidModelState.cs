using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using WebWallet.API.v1.Models;

namespace WebWallet.API.ModelValidation
{
    internal class InvalidModelState : ErrorModel
    {
        public InvalidModelState(ModelStateDictionary modelState) : base("Model validation failed.")
        {
            Errors = modelState.SelectMany(x => x.Value.Errors.Select(error => new FieldValidationError(x.Key, error.ErrorMessage))).ToList();
        }

        public ICollection<FieldValidationError> Errors { get; }
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