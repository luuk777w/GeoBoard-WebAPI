using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Responses
{
    // Source: https://www.devtrends.co.uk/blog/handling-errors-in-asp.net-core-web-api
    public class BadRequestHttpResponseModel : HttpResponseModel
    {
        /// <summary>
        /// A collecton of errors to return.
        /// </summary>
        public Dictionary<string, string[]> Errors { get; }

        /// <summary>
        /// Return an empty Bad Request response.
        /// </summary>
        public BadRequestHttpResponseModel() : base(400)
        {
        }

        /// <summary>
        /// Construct a Bad Request response with a message.
        /// </summary>
        /// <param name="message">The message to return.</param>
        public BadRequestHttpResponseModel(string message) : base(400, message)
        {
        }

        /// <summary>
        /// Construct an Api Bad Request response using a ModelState.
        /// </summary>
        /// <param name="modelState"></param>
        public BadRequestHttpResponseModel(ModelStateDictionary modelState) : base (400)
        {
            // Only invalid modelstates can be handled by this response.
            if (modelState.IsValid)
            {
                throw new ArgumentException("ModelState must be invalid", nameof(modelState));
            }

            // Fill the errors collection.
            // Source: https://stackoverflow.com/a/2845864/3625118
            Errors = modelState.ToDictionary(
               kvp => kvp.Key,
               kvp => kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
            );
        }
    }
}
