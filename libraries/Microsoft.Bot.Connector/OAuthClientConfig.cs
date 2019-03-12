﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Microsoft.Bot.Connector
{
    public static class OAuthClientConfig
    {
        /// <summary>
        /// Gets or sets The default endpoint that is used for API requests.
        /// </summary>
        /// <value>OAuthEndpoint.</value>
        public static string OAuthEndpoint { get; set; } = AuthenticationConstants.OAuthUrl;

        /// <summary>
        /// Gets or sets a value indicating whether gets or Sets When using the Emulator, whether to emulate the OAuthCard behavior or use connected flows.
        /// </summary>
        /// <value>EmulateOAuthCards.</value>
        public static bool EmulateOAuthCards { get; set; } = false;

        /// <summary>
        /// Send a dummy OAuth card when the bot is being used on the Emulator for testing without fetching a real token.
        /// </summary>
        /// <param name="client">client.</param>
        /// <param name="emulateOAuthCards">Indicates whether the Emulator should emulate the OAuth card.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public static async Task SendEmulateOAuthCardsAsync(OAuthClient client, bool emulateOAuthCards)
        {
            // Tracing
            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString();
                Dictionary<string, object> tracingParameters = new Dictionary<string, object>();
                tracingParameters.Add("emulateOAuthCards", emulateOAuthCards);
                ServiceClientTracing.Enter(invocationId, client, "GetToken", tracingParameters);
            }

            // Construct URL
            var baseUrl = client.BaseUri.AbsoluteUri;
            var url = new Uri(new Uri(baseUrl + (baseUrl.EndsWith("/") ? string.Empty : "/")), "api/usertoken/emulateOAuthCards?emulate={emulate}").ToString();
            url = url.Replace("{emulate}", emulateOAuthCards.ToString());

            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage();
            HttpResponseMessage httpResponse = null;
            httpRequest.Method = new HttpMethod("POST");
            httpRequest.RequestUri = new System.Uri(url);

            var cancellationToken = CancellationToken.None;

            // Serialize Request
            string requestContent = null;

            // Set Credentials
            if (client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }

            // Send Request
            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }

            cancellationToken.ThrowIfCancellationRequested();
            httpResponse = await client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, httpResponse);
            }

            HttpStatusCode statusCode = httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            string responseContent = null;
            if ((int)statusCode != 200 && (int)statusCode != 404)
            {
                var ex = new ErrorResponseException(string.Format("Operation returned an invalid status code '{0}'", statusCode));
                try
                {
                    responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    ErrorResponse errorBody = Rest.Serialization.SafeJsonConvert.DeserializeObject<ErrorResponse>(responseContent, client.DeserializationSettings);
                    if (errorBody != null)
                    {
                        ex.Body = errorBody;
                    }
                }
                catch (JsonException)
                {
                    // Ignore the exception
                }

                ex.Request = new HttpRequestMessageWrapper(httpRequest, requestContent);
                ex.Response = new HttpResponseMessageWrapper(httpResponse, responseContent);
                if (shouldTrace)
                {
                    ServiceClientTracing.Error(invocationId, ex);
                }

                httpRequest.Dispose();
                if (httpResponse != null)
                {
                    httpResponse.Dispose();
                }

                throw ex;
            }

            // Create Result
            var result = new HttpOperationResponse<int>();
            result.Request = httpRequest;
            result.Response = httpResponse;

            if (shouldTrace)
            {
                ServiceClientTracing.Exit(invocationId, result);
            }
        }
    }
}
