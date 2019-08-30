﻿// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Thrzn41.WebexTeams.Version1;

namespace Microsoft.Bot.Builder.Adapters.Webex
{
    public class WebexAdapter : BotAdapter
    {
        private readonly WebexAdapterOptions _config;

        private readonly WebexClientWrapper _webexClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebexAdapter"/> class.
        /// Creates a Webex adapter. See <see cref="WebexAdapterOptions"/> for a full definition of the allowed parameters.
        /// </summary>
        /// <param name="config">An object containing API credentials, a webhook verification token and other options.</param>
        /// <param name="webexClient">A Webex API interface.</param>
        public WebexAdapter(WebexAdapterOptions config, WebexClientWrapper webexClient)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            if (string.IsNullOrWhiteSpace(_config.AccessToken))
            {
                throw new Exception("AccessToken required to create controller");
            }

            if (string.IsNullOrWhiteSpace(_config.PublicAddress))
            {
                throw new Exception("PublicAddress parameter required to receive webhooks");
            }

            _config.PublicAddress = new Uri(_config.PublicAddress).Host;

            _webexClient = webexClient ?? throw new Exception("Could not create the Webex Teams API client");

            _webexClient.CreateClient(_config.AccessToken);
        }

        /// <summary>
        /// Load the bot's identity via the WebEx API.
        /// MUST be called by BotBuilder bots in order to filter messages sent by the bot.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task GetIdentityAsync()
        {
            await _webexClient.GetMeAsync().ContinueWith(
                task => { WebexHelper.Identity = task.Result; }, TaskScheduler.Current).ConfigureAwait(false);
        }

        /// <summary>
        /// Lists all webhook subscriptions currently associated with this application.
        /// </summary>
        /// <returns>A list of webhook subscriptions.</returns>
        public async Task<WebhookList> ListWebhookSubscriptionsAsync()
        {
           return await _webexClient.ListWebhooksAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Clears out and resets the list of webhook subscriptions.
        /// </summary>
        /// <param name="webhookList">List of webhook subscriptions to be deleted.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ResetWebhookSubscriptionsAsync(WebhookList webhookList)
        {
            for (var i = 0; i < webhookList.ItemCount; i++)
            {
                await _webexClient.DeleteWebhookAsync(webhookList.Items[i]).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Register a webhook subscription with Webex Teams to start receiving message events.
        /// </summary>
        /// <param name="webhookPath">The path of the webhook endpoint like '/api/messages'.</param>
        /// <param name="webhookList">List of webhook subscriptions associated with the application.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Webhook> RegisterWebhookSubscriptionAsync(string webhookPath, WebhookList webhookList)
        {
            var webHookName = _config.WebhookName ?? "Webex Firehose";

            string hookId = null;

            for (var i = 0; i < webhookList.ItemCount; i++)
            {
                if (webhookList.Items[i].Name == webHookName)
                {
                    hookId = webhookList.Items[i].Id;
                }
            }

            var hookUrl = "https://" + _config.PublicAddress + webhookPath;
            Webhook webhook = null;

            if (hookId != null)
            {
                webhook = await _webexClient.UpdateWebhookAsync(hookId, webHookName, new Uri(hookUrl), _config.Secret).ConfigureAwait(false);
            }
            else
            {
                webhook = await _webexClient.CreateWebhookAsync(webHookName, new Uri(hookUrl), EventResource.All, EventType.All, null, _config.Secret).ConfigureAwait(false);
            }

            return webhook;
        }

        /// <summary>
        /// Standard BotBuilder adapter method to send a message from the bot to the messaging API.
        /// </summary>
        /// <param name="turnContext">A TurnContext representing the current incoming message and environment.</param>
        /// <param name="activities">An array of outgoing activities to be sent back to the messaging API.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task<ResourceResponse[]> SendActivitiesAsync(ITurnContext turnContext, Activity[] activities, CancellationToken cancellationToken)
        {
            var responses = new List<ResourceResponse>();
            foreach (var activity in activities)
            {
                if (activity.Type != ActivityTypes.Message)
                {
                    throw new Exception("Unknown message type");
                }

                // transform activity into the webex message format
                var personIdOrEmail = string.Empty;

                if (activity.GetChannelData<WebhookEventData>()?.MessageData.PersonEmail != null)
                {
                    personIdOrEmail = activity.GetChannelData<WebhookEventData>()?.MessageData.PersonEmail;
                }
                else
                {
                    if (activity.Recipient?.Id != null)
                    {
                        personIdOrEmail = activity.Recipient.Id;
                    }
                    else
                    {
                        throw new Exception("No Person or Email to send the message");
                    }
                }

                var files = new List<Uri>();

                if (activity.Attachments != null)
                {
                    foreach (var attachment in activity.Attachments)
                    {
                        var file = new Uri(attachment.ContentUrl);

                        files.Add(file);
                    }
                }

                var responseId = await _webexClient.CreateMessageAsync(personIdOrEmail, activity.Text, files.Count > 0 ? files : null).ConfigureAwait(false);
                responses.Add(new ResourceResponse(responseId));
            }

            return responses.ToArray();
        }

        /// <summary>
        /// Standard BotBuilder adapter method to update a previous message.
        /// </summary>
        /// <param name="turnContext">A TurnContext representing the current incoming message and environment.</param>
        /// <param name="activity">An activity to be sent back to the messaging API.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override Task<ResourceResponse> UpdateActivityAsync(ITurnContext turnContext, Activity activity, CancellationToken cancellationToken)
        {
            // Webex adapter does not support updateActivity.
            return Task.FromException<ResourceResponse>(new NotSupportedException("Webex adapter does not support updateActivity."));
        }

        /// <summary>
        /// Standard BotBuilder adapter method to delete a previous message.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> representing the current incoming message and environment.</param>
        /// <param name="reference">A <see cref="ConversationReference"/> object.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public override async Task DeleteActivityAsync(ITurnContext turnContext, ConversationReference reference, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(reference.ActivityId))
            {
                await _webexClient.DeleteMessageAsync(reference.ActivityId).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Standard BotBuilder adapter method for continuing an existing conversation based on a conversation reference.
        /// </summary>
        /// <param name="reference">A <see cref="ConversationReference"/> to be applied to future messages.</param>
        /// <param name="logic">A bot logic function that will perform continuing action.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ContinueConversationAsync(ConversationReference reference, BotCallbackHandler logic, CancellationToken cancellationToken)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (logic == null)
            {
                throw new ArgumentNullException(nameof(logic));
            }

            var request = reference.GetContinuationActivity().ApplyConversationReference(reference, true);

            using (var context = new TurnContext(this, request))
            {
                await RunPipelineAsync(context, logic, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Accept an incoming webhook <see cref="HttpRequest"/> and convert it into a <see cref="TurnContext"/> which can be processed by the bot's logic.
        /// </summary>
        /// <param name="request">A <see cref="HttpRequest"/> object.</param>
        /// <param name="response">A <see cref="HttpResponse"/> object.</param>
        /// <param name="bot">A bot with logic function in the form.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ProcessAsync(HttpRequest request, HttpResponse response, IBot bot, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (bot == null)
            {
                throw new ArgumentNullException(nameof(bot));
            }

            WebhookEventData payload;
            string json = null;

            using (var bodyStream = new StreamReader(request.Body))
            {
                json = bodyStream.ReadToEnd();
                payload = JsonConvert.DeserializeObject<WebhookEventData>(json);
            }

            if (!string.IsNullOrWhiteSpace(_config.Secret))
            {
                if (!WebexHelper.ValidateSignature(_config.Secret, request, json))
                {
                    throw new Exception("WARNING: Webhook received message with invalid signature. Potential malicious behavior!");
                }
            }

            Activity activity;

            if (payload.Resource == EventResource.Message && payload.EventType == EventType.Created)
            {
                Message decryptedMessage = await WebexHelper.GetDecryptedMessageAsync(payload, _webexClient.GetMessageAsync).ConfigureAwait(false);

                activity = WebexHelper.DecryptedMessageToActivity(decryptedMessage);
            }
            else
            {
                activity = WebexHelper.PayloadToActivity(payload);
            }

            using (var context = new TurnContext(this, activity))
            {
                await RunPipelineAsync(context, bot.OnTurnAsync, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
