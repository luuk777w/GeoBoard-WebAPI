
using Microsoft.Extensions.Localization;
using GeoBoardWebAPI.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
namespace GeoBoardWebAPI.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class AppRequiredAttribute : RequiredAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var stringLocalizer = validationContext.GetService(typeof(IStringLocalizer<SharedDataAnnotationResources>)) as IStringLocalizer<SharedDataAnnotationResources>;
            ErrorMessage = stringLocalizer["The {0} field is required."]?.Value ?? "The {0} field is required.";

            return base.IsValid(value, validationContext);
        }
    }
}

