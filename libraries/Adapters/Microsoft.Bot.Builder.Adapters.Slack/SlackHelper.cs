﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SlackAPI;

#if SIGNASSEMBLY
[assembly: InternalsVisibleTo("Microsoft.Bot.Builder.Adapters.Twilio.Tests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]
#else
[assembly: InternalsVisibleTo("Microsoft.Bot.Builder.Adapters.Slack.Tests")]
#endif

namespace Microsoft.Bot.Builder.Adapters.Slack
{
    internal static class SlackHelper
    {
        /// <summary>
        /// Formats a BotBuilder activity into an outgoing Slack message.
        /// </summary>
        /// <param name="activity">A BotBuilder Activity object.</param>
        /// <returns>A Slack message object with {text, attachments, channel, thread ts} as well as any fields found in activity.channelData.</returns>
        public static NewSlackMessage ActivityToSlack(Activity activity)
        {
            if (activity == null)
            {
                return null;
            }

            var message = new NewSlackMessage();

            if (activity.Timestamp != null)
            {
                message.Ts = activity.Timestamp.Value.DateTime.ToString(CultureInfo.InvariantCulture);
            }

            message.Text = activity.Text;

            if (activity.Attachments != null)
            {
                var attachments = new List<SlackAPI.Attachment>();

                foreach (var att in activity.Attachments)
                {
                    if (att.Name == "blocks")
                    {
                        message.Blocks = (List<Block>)att.Content;
                    }
                    else
                    {
                        var newAttachment = new SlackAPI.Attachment()
                        {
                            author_name = att.Name, thumb_url = att.ThumbnailUrl,
                        };
                        attachments.Add(newAttachment);
                    }
                }

                if (attachments.Count > 0)
                {
                    message.Attachments = attachments;
                }
            }

            message.Channel = activity.Conversation.Id;

            if (!string.IsNullOrWhiteSpace(activity.Conversation.Properties["thread_ts"]?.ToString()))
            {
                message.ThreadTs = activity.Conversation.Properties["thread_ts"].ToString();
            }

            // if channelData is specified, overwrite any fields in message object
            if (activity.ChannelData != null)
            {
                message = activity.GetChannelData<NewSlackMessage>();
            }

            // should this message be sent as an ephemeral message
            if (!string.IsNullOrWhiteSpace(message.Ephemeral))
            {
                message.User = activity.Recipient.Id;
            }

            if (message.IconUrl != null || !string.IsNullOrWhiteSpace(message.Icons?.status_emoji) || !string.IsNullOrWhiteSpace(message.Username))
            {
                message.AsUser = false;
            }

            return message;
        }

        /// <summary>
        /// Writes the HttpResponse.
        /// </summary>
        /// <param name="response">The httpResponse.</param>
        /// <param name="code">The status code to be written.</param>
        /// <param name="text">The text to be written.</param>
        /// <param name="encoding">The encoding for the text.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static async Task WriteAsync(HttpResponse response, HttpStatusCode code, string text, Encoding encoding, CancellationToken cancellationToken = default)
        {
            if (response == null)
            {
                throw new ArgumentNullException(nameof(response));
            }

            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            response.ContentType = "text/plain";
            response.StatusCode = (int)code;

            var data = encoding.GetBytes(text);

            await response.Body.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates an activity based on the slack event payload.
        /// </summary>
        /// <param name="slack">The payload of the slack event.</param>
        /// <returns>An activity containing the event data.</returns>
        public static Activity PayloadToActivity(SlackPayload slack)
        {
            if (slack == null)
            {
                return null;
            }

            var activity = new Activity()
            {
                Timestamp = default(DateTime),
                ChannelId = "slack",
                Conversation = new ConversationAccount()
                {
                    Id = slack.Channel.id,
                },
                From = new ChannelAccount()
                {
                    Id = slack.Message.BotId ?? slack.User.id,
                },
                Recipient = new ChannelAccount()
                {
                    Id = null,
                },
                ChannelData = slack,
                Text = null,
                Type = ActivityTypes.Event,
            };

            if (slack.ThreadTs != null)
            {
                activity.Conversation.Properties["thread_ts"] = slack.ThreadTs;
            }

            if (slack.Actions != null && (slack.Type == "block_actions" || slack.Type == "interactive_message"))
            {
                activity.Type = ActivityTypes.Message;
                activity.Text = slack.Actions[0].Value;
            }

            return activity;
        }

        /// <summary>
        /// Creates an activity based on the slack event data.
        /// </summary>
        /// <param name="slack">The data of the slack event.</param>
        /// <param name="client">The Slack client.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>An activity containing the event data.</returns>
        public static async Task<Activity> EventToActivityAsync(SlackEvent slack, SlackClientWrapper client, CancellationToken cancellationToken)
        {
            if (slack == null)
            {
                return null;
            }

            var activity = new Activity()
            {
                Id = slack.EventTs,
                Timestamp = default(DateTime),
                ChannelId = "slack",
                Conversation = new ConversationAccount()
                {
                    Id = slack.Channel ?? slack.ChannelId,
                },
                From = new ChannelAccount()
                {
                    Id = slack.BotId ?? slack.UserId,
                },
                Recipient = new ChannelAccount()
                {
                    Id = null,
                },
                ChannelData = slack,
                Text = null,
                Type = ActivityTypes.Event,
            };

            if (slack.ThreadTs != null)
            {
                activity.Conversation.Properties["thread_ts"] = slack.ThreadTs;
            }

            if (activity.Conversation.Id == null)
            {
                if (slack.Item != null && slack.ItemChannel != null)
                {
                    activity.Conversation.Id = slack.ItemChannel;
                }
                else
                {
                    activity.Conversation.Id = slack.Team;
                }
            }

            activity.Recipient.Id = await client.GetBotUserByTeamAsync(activity, cancellationToken).ConfigureAwait(false);

            // If this is conclusively a message originating from a user, we'll mark it as such
            if (slack.Type == "message" && slack.SubType == null)
            {
                activity.Type = ActivityTypes.Message;
                activity.Text = slack.Text;
            }

            return activity;
        }

        /// <summary>
        /// Creates an activity based on a slack event related to a slash command.
        /// </summary>
        /// <param name="slack">The data of the slack event.</param>
        /// <param name="client">The Slack client.</param>
        /// <param name="cancellationToken">A cancellation token for the task.</param>
        /// <returns>An activity containing the event data.</returns>
        public static async Task<Activity> CommandToActivityAsync(SlackRequestBody slack, SlackClientWrapper client, CancellationToken cancellationToken)
        {
            if (slack == null)
            {
                return null;
            }

            var activity = new Activity()
            {
                Id = slack.TriggerId,
                Timestamp = default(DateTime),
                ChannelId = "slack",
                Conversation = new ConversationAccount()
                {
                    Id = slack.ChannelId,
                },
                From = new ChannelAccount()
                {
                    Id = slack.UserId,
                },
                Recipient = new ChannelAccount()
                {
                    Id = null,
                },
                ChannelData = slack,
                Text = slack.Text,
                Type = ActivityTypes.Event,
            };

            activity.Recipient.Id = await client.GetBotUserByTeamAsync(activity, cancellationToken).ConfigureAwait(false);

            activity.Conversation.Properties["team"] = slack.TeamId;

            return activity;
        }

        /// <summary>
        /// Converts a query string to a dictionary with key-value pairs.
        /// </summary>
        /// <param name="query">The query string to convert.</param>
        /// <returns>A dictionary with the query values.</returns>
        public static Dictionary<string, string> QueryStringToDictionary(string query)
        {
            var values = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(query))
            {
                return values;
            }

            var pairs = query.Replace("+", "%20").Split('&');

            foreach (var p in pairs)
            {
                var pair = p.Split('=');
                var key = pair[0];
                var value = Uri.UnescapeDataString(pair[1]);

                values.Add(key, value);
            }

            return values;
        }

        /// <summary>
        /// Deserializes the request's body as a <see cref="SlackRequestBody"/> object.
        /// </summary>
        /// <param name="requestBody">The query string to convert.</param>
        /// <returns>A dictionary with the query values.</returns>
        public static SlackRequestBody DeserializeBody(string requestBody)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return null;
            }

            // Check if it's a command event
            if (requestBody.Contains("command=%2F"))
            {
                var commandBody = QueryStringToDictionary(requestBody);

                return JsonConvert.DeserializeObject<SlackRequestBody>(JsonConvert.SerializeObject(commandBody));
            }

            if (requestBody.Contains("payload="))
            {
                // Decode and remove "payload=" from the body
                var decodedBody = Uri.UnescapeDataString(requestBody).Remove(0, 8);

                var payload = JsonConvert.DeserializeObject<SlackPayload>(decodedBody);

                return new SlackRequestBody
                {
                    Payload = payload,
                    Token = payload.Token,
                };
            }

            return JsonConvert.DeserializeObject<SlackRequestBody>(requestBody, new UnixDateTimeConverter());
        }
    }
}
