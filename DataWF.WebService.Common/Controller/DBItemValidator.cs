using DataWF.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.Net;

namespace DataWF.WebService.Common
{
    public class DBItemValidator : IObjectModelValidator, IClientModelValidator
    {
        public void AddValidation(ClientModelValidationContext context)
        {

        }

        public void Validate(ActionContext actionContext, ValidationStateDictionary validationState, string prefix, object model)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                if (actionContext.ModelState.TryGetValue(prefix, out var validator))
                {
                    validator.ValidationState = ModelValidationState.Valid;
                }
            }
            //if (model is DBItem)
            //{
            //}
        }
    }
}