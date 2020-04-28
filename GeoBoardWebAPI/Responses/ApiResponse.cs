using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GeoBoardWebAPI.Responses
{
    // Source: https://www.devtrends.co.uk/blog/handling-errors-in-asp.net-core-web-api
    public class ApiResponse
    {
        /// <summary>
        /// The HTTP Status Code
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// An optional status message.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; }

        /// <summary>
        /// Construct the API response.
        /// </summary>
        /// <param name="statusCode">The status code to return.</param>
        /// <param name="message">An optional message to return.</param>
        public ApiResponse(int statusCode, string message = null)
        {
            StatusCode = statusCode;
            Message = message ?? GetDefaultMessageForStatusCode(statusCode);
        }

        /// <summary>
        /// Get the default error message for the given status code (when available).
        /// </summary>
        /// <param name="statusCode">The status code to return a message for.</param>
        private static string GetDefaultMessageForStatusCode(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    return "Resource not found";
                case 500:
                    return "An unhandled error occurred";
                default:
                    return null;
            }
        }
    }
}
