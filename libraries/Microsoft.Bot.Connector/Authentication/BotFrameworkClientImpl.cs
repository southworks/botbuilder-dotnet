﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Connector.Authentication
{
    internal class BotFrameworkClientImpl : BotFrameworkClient
    {
        private readonly ServiceClientCredentialsFactory _credentialsFactory;
        private readonly HttpClient _httpClient;
        private readonly string _loginEndpoint;
        private readonly ILogger _logger;
        private bool _disposed;

        public BotFrameworkClientImpl(
            ServiceClientCredentialsFactory credentialsFactory,
            IHttpClientFactory httpClientFactory,
            string loginEndpoint,
            ILogger logger)
        {
            _credentialsFactory = credentialsFactory;
            _httpClient = httpClientFactory?.CreateClient() ?? new HttpClient();
            _loginEndpoint = loginEndpoint;
            _logger = logger ?? NullLogger.Instance;
            ConnectorClient.AddDefaultRequestHeaders(_httpClient);
        }

        public async override Task<InvokeResponse<T>> PostActivityAsync<T>(string fromBotId, string toBotId, Uri toUrl, Uri serviceUrl, string conversationId, Activity activity, CancellationToken cancellationToken = default)
        {
            _ = fromBotId ?? throw new ArgumentNullException(nameof(fromBotId));
            _ = toBotId ?? throw new ArgumentNullException(nameof(toBotId));
            _ = toUrl ?? throw new ArgumentNullException(nameof(toUrl));
            _ = serviceUrl ?? throw new ArgumentNullException(nameof(serviceUrl));
            _ = conversationId ?? throw new ArgumentNullException(nameof(conversationId));
            _ = activity ?? throw new ArgumentNullException(nameof(activity));

            Log.PostToSkill(_logger, toBotId, toUrl);

            var credentials = await _credentialsFactory.CreateCredentialsAsync(fromBotId, toBotId, _loginEndpoint, true, cancellationToken).ConfigureAwait(false);

            // Clone the activity so we can modify it before sending without impacting the original object.
            var activityClone = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(activity));

            // Apply the appropriate addressing to the newly created Activity.
            var conversation = new ConversationAccount
            {
                Id = activityClone.Conversation.Id,
                Name = activityClone.Conversation.Name,
                ConversationType = activityClone.Conversation.ConversationType,
                AadObjectId = activityClone.Conversation.AadObjectId,
                IsGroup = activityClone.Conversation.IsGroup,
                Role = activityClone.Conversation.Role,
                TenantId = activityClone.Conversation.TenantId,
            };
            conversation.Properties.Merge(activityClone.Conversation.Properties);

            activityClone.RelatesTo = new ConversationReference
            {
                ServiceUrl = activityClone.ServiceUrl,
                ActivityId = activityClone.Id,
                ChannelId = activityClone.ChannelId,
                Locale = activityClone.Locale,
                Conversation = conversation,
            };
            activityClone.Conversation.Id = conversationId;
            activityClone.ServiceUrl = serviceUrl.ToString();
            activityClone.Recipient ??= new ChannelAccount();
            activityClone.Recipient.Role = RoleTypes.Skill;

            // Create the HTTP request from the cloned Activity and send it to the Skill.
            using (var jsonContent = new StringContent(JsonConvert.SerializeObject(activityClone, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }), Encoding.UTF8, "application/json"))
            {
                using (var httpRequestMessage = new HttpRequestMessage())
                {
                    httpRequestMessage.Method = HttpMethod.Post;
                    httpRequestMessage.RequestUri = toUrl;
                    httpRequestMessage.Content = jsonContent;

                    httpRequestMessage.Headers.Add(ConversationConstants.ConversationIdHttpHeaderName, conversationId);

                    // Add the auth header to the HTTP request.
                    await credentials.ProcessHttpRequestAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false);

                    using (var httpResponseMessage = await _httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
                    {
                        var content = httpResponseMessage.Content != null ? await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null;

                        if (httpResponseMessage.IsSuccessStatusCode)
                        {
                            // On success assuming either JSON that can be deserialized to T or empty.
                            return new InvokeResponse<T>
                            {
                                Status = (int)httpResponseMessage.StatusCode,
                                Body = content?.Length > 0 ? JsonConvert.DeserializeObject<T>(content) : default
                            };
                        }
                        else
                        {
                            // Otherwise we can assume we don't have a T to deserialize - so just log the content so it's not lost.
                            Log.PostToSkillFailed(_logger, toUrl, (int)httpResponseMessage.StatusCode, content);

                            // We want to at least propogate the status code because that is what InvokeResponse expects.
                            return new InvokeResponse<T>
                            {
                                Status = (int)httpResponseMessage.StatusCode,
                                Body = typeof(T) == typeof(object) ? (T)(object)content : default,
                            };
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _httpClient.Dispose();
            base.Dispose(disposing);
            _disposed = true;
        }

        /// <summary>
        /// Log messages for <see cref="BotFrameworkClientImpl"/>.
        /// </summary>
        /// <remarks>
        /// Messages implemented using <see cref="LoggerMessage.Define(LogLevel, EventId, string)"/> to maximize performance.
        /// For more information, see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/loggermessage?view=aspnetcore-5.0.
        /// </remarks>
        private static class Log
        {
            private static readonly Action<ILogger, string, Uri, Exception> _postToSkill =
                LoggerMessage.Define<string, Uri>(LogLevel.Information, new EventId(1, nameof(PostToSkill)), "Post to skill '{String}' at '{Uri}'.");

            private static readonly Action<ILogger, Uri, int, string, Exception> _postToSkillFailed =
                LoggerMessage.Define<Uri, int, string>(LogLevel.Error, new EventId(2, nameof(PostToSkillFailed)), "Bot Framework call failed to '{Uri}' returning '{Int32}' and '{String}'.");

            public static void PostToSkill(ILogger logger, string botId, Uri url) => _postToSkill(logger, botId, url, null);

            public static void PostToSkillFailed(ILogger logger, Uri url, int statusCode, string content) => _postToSkillFailed(logger, url, statusCode, content, null);
        }
    }
}
