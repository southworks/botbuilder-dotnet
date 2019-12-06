﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Integration.AspNet.Core
{
    public class BotFrameworkHttpClient
    {
        public BotFrameworkHttpClient(
            HttpClient httpClient,
            ICredentialProvider credentialProvider,
            IChannelProvider channelProvider = null,
            ILogger logger = null)
        {
            HttpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            CredentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
            ChannelProvider = channelProvider;
            Logger = logger ?? NullLogger.Instance;
            ConnectorClient.AddDefaultRequestHeaders(HttpClient);
        }

        // Cache for appCredentials to speed up token acquisition (a token is not requested unless is expired)
        // AppCredentials are cached using appId + scope (this last parameter is only used if the app credentials are used to call a skill)
        protected static ConcurrentDictionary<string, AppCredentials> AppCredentialMapCache { get; private set; } = new ConcurrentDictionary<string, AppCredentials>();

        protected IChannelProvider ChannelProvider { get; private set; }

        protected ICredentialProvider CredentialProvider { get; private set; }

        protected HttpClient HttpClient { get; private set; }

        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Forwards an activity to a skill (bot).
        /// </summary>
        /// <remarks>NOTE: Forwarding an activity to a skill will flush UserState and ConversationState changes so that skill has accurate state.</remarks>
        /// <param name="fromBotId">The MicrosoftAppId of the bot sending the activity.</param>
        /// <param name="toBotId">The MicrosoftAppId of the bot receiving the activity.</param>
        /// <param name="toUrl">The URL of the bot receiving the activity.</param>
        /// <param name="serviceUrl">The callback Url for the skill host.</param>
        /// <param name="conversationId">A conversation ID to use for the conversation with the skill.</param>
        /// <param name="activity">activity to forward.</param>
        /// <param name="cancellationToken">cancellation Token.</param>
        /// <returns>Async task with optional invokeResponse.</returns>
        public async Task<InvokeResponse> PostActivityAsync(string fromBotId, string toBotId, Uri toUrl, Uri serviceUrl, string conversationId, Activity activity, CancellationToken cancellationToken = default)
        {
            var appCredentials = await GetAppCredentialsAsync(fromBotId, toBotId).ConfigureAwait(false);
            if (appCredentials == null)
            {
                throw new InvalidOperationException("Unable to get appCredentials to connect to the skill");
            }

            // Get token for the skill call
            var token = appCredentials == MicrosoftAppCredentials.Empty ? null : await appCredentials.GetTokenAsync().ConfigureAwait(false);

            // Capture current activity settings before changing them.
            // TODO: DO we need to set the activity ID? (events that are created manually don't have it).
            var originalConversationId = activity.Conversation.Id;
            var originalServiceUrl = activity.ServiceUrl;
            try
            {
                activity.Conversation.Id = conversationId;
                activity.ServiceUrl = serviceUrl.ToString();

                using (var jsonContent = new StringContent(JsonConvert.SerializeObject(activity, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }), Encoding.UTF8, "application/json"))
                {
                    using (var httpRequestMessage = new HttpRequestMessage())
                    {
                        httpRequestMessage.Method = HttpMethod.Post;
                        httpRequestMessage.RequestUri = toUrl;
                        if (token != null)
                        {
                            httpRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                        }

                        httpRequestMessage.Content = jsonContent;

                        var response = await HttpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);

                        var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        return new InvokeResponse
                        {
                            Status = (int)response.StatusCode,
                            Body = content.Length > 0 ? JsonConvert.DeserializeObject(content) : null
                        };
                    }
                }
            }
            finally
            {
                // Restore activity properties.
                activity.Conversation.Id = originalConversationId;
                activity.ServiceUrl = originalServiceUrl;
            }
        }

        /// <summary>
        /// Logic to build an <see cref="AppCredentials"/> object to be used to acquire tokens
        /// for this HttpClient.
        /// </summary>
        /// <param name="appId">The application id.</param>
        /// <param name="oAuthScope">The optional OAuth scope.</param>
        /// <returns>The app credentials to be used to acquire tokens.</returns>
        protected virtual async Task<AppCredentials> BuildCredentialsAsync(string appId, string oAuthScope = null)
        {
            var appPassword = await CredentialProvider.GetAppPasswordAsync(appId).ConfigureAwait(false);
            return ChannelProvider != null && ChannelProvider.IsGovernment() ? new MicrosoftGovernmentAppCredentials(appId, appPassword, HttpClient, Logger) : new MicrosoftAppCredentials(appId, appPassword, HttpClient, Logger, oAuthScope);
        }

        /// <summary>
        /// Gets the application credentials. App Credentials are cached so as to ensure we are not refreshing
        /// token every time.
        /// </summary>
        /// <param name="appId">The application identifier (AAD Id for the bot).</param>
        /// <param name="oAuthScope">The scope for the token, skills will use the Skill App Id. </param>
        /// <returns>App credentials.</returns>
        private async Task<AppCredentials> GetAppCredentialsAsync(string appId, string oAuthScope = null)
        {
            if (appId == null)
            {
                return MicrosoftAppCredentials.Empty;
            }

            // If the credentials are in the cache, retrieve them from there
            var cacheKey = $"{appId}{oAuthScope}";
            if (AppCredentialMapCache.TryGetValue(cacheKey, out var appCredentials))
            {
                return appCredentials;
            }

            // Credentials not found in cache, build them
            appCredentials = await BuildCredentialsAsync(appId, oAuthScope).ConfigureAwait(false);

            // Cache the credentials for later use
            AppCredentialMapCache[cacheKey] = appCredentials;
            return appCredentials;
        }
    }
}
