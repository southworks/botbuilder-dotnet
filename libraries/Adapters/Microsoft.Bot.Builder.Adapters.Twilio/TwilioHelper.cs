﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Security;

using AuthenticationException = System.Security.Authentication.AuthenticationException;

[assembly: InternalsVisibleTo("Microsoft.Bot.Builder.Adapters.Twilio.Tests")]

namespace Microsoft.Bot.Builder.Adapters.Twilio
{
    /// <summary>
    /// A helper class to create Activities and Twilio messages.
    /// </summary>
    internal static class TwilioHelper
    {
        /// <summary>
        /// Formats a BotBuilder activity into an outgoing Twilio SMS message.
        /// </summary>
        /// <param name="activity">A BotBuilder Activity object.</param>
        /// <param name="twilioNumber">The assigned Twilio phone number.</param>
        /// <returns>A Message's options object with {body, from, to, mediaUrl}.</returns>
        public static CreateMessageOptions ActivityToTwilio(Activity activity, string twilioNumber)
        {
            if (activity == null || string.IsNullOrWhiteSpace(twilioNumber))
            {
                return null;
            }

            var mediaUrls = new List<Uri>();
            if (activity.Attachments != null)
            {
                mediaUrls.AddRange(activity.Attachments.Select(attachment => new Uri(attachment.ContentUrl)));
            }

            var messageOptions = new CreateMessageOptions(activity.Conversation.Id)
            {
                ApplicationSid = activity.Conversation.Id,
                From = twilioNumber,
                Body = activity.Text,
                MediaUrl = mediaUrls,
            };

            return messageOptions;
        }

        /// <summary>
        /// Processes a HTTP request into an Activity.
        /// </summary>
        /// <param name="httpRequest">A httpRequest object.</param>
        /// <param name="validationUrl">The URL to check the validation against.</param>
        /// <param name="authToken">The authentication token for the Twilio app.</param>
        /// <returns>The Activity obtained from the httpRequest object.</returns>
        public static Activity RequestToActivity(HttpRequest httpRequest, string validationUrl, string authToken)
        {
            if (httpRequest == null)
            {
                return null;
            }

            Dictionary<string, string> body;
            using (var bodyStream = new StreamReader(httpRequest.Body))
            {
                body = QueryStringToDictionary(bodyStream.ReadToEnd());
            }

            ValidateRequest(httpRequest, body, validationUrl, authToken);

            var twilioMessage = JsonConvert.DeserializeObject<TwilioMessage>(JsonConvert.SerializeObject(body));

            return new Activity()
            {
                Id = twilioMessage.MessageSid,
                Timestamp = DateTime.UtcNow,
                ChannelId = Channels.Twilio,
                Conversation = new ConversationAccount()
                {
                    Id = twilioMessage.From ?? twilioMessage.Author,
                },
                From = new ChannelAccount()
                {
                    Id = twilioMessage.From ?? twilioMessage.Author,
                },
                Recipient = new ChannelAccount()
                {
                    Id = twilioMessage.To,
                },
                Text = twilioMessage.Body,
                ChannelData = twilioMessage,
                Type = ActivityTypes.Message,
                Attachments = int.TryParse(twilioMessage.NumMedia, out var numMediaResult) && numMediaResult > 0 ? GetMessageAttachments(numMediaResult, body) : null,
            };
        }

        /// <summary>
        /// Validates a request as coming from Twilio.
        /// </summary>
        /// <param name="httpRequest">The request to validate.</param>
        /// <param name="body">The stringified body payload of the request.</param>
        /// <param name="validationUrl">The URL to check the validation against.</param>
        /// <param name="authToken">The authentication token for the Twilio app.</param>
        private static void ValidateRequest(HttpRequest httpRequest, Dictionary<string, string> body, string validationUrl, string authToken)
        {
            var twilioSignature = httpRequest.Headers["x-twilio-signature"];
            validationUrl = validationUrl ?? (httpRequest.Headers["x-forwarded-proto"][0] ?? httpRequest.Protocol + "://" + httpRequest.Host + httpRequest.Path);
            var requestValidator = new RequestValidator(authToken);
            if (!requestValidator.Validate(validationUrl, body, twilioSignature))
            {
                throw new AuthenticationException("Request does not match provided signature");
            }
        }

        /// <summary>
        /// Extracts attachments (if any) from a twilio message and returns them in an Attachments array.
        /// </summary>
        /// <param name="numMedia">The number of media items to pull from the message body.</param>
        /// <param name="message">A dictionary containing the twilio message elements.</param>
        /// <returns>An Attachments array with the converted attachments.</returns>
        private static List<Attachment> GetMessageAttachments(int numMedia, Dictionary<string, string> message)
        {
            var attachments = new List<Attachment>();
            for (var i = 0; i < numMedia; i++)
            {
                var attachment = new Attachment()
                {
                    ContentType = message[$"MediaContentType{i}"],
                    ContentUrl = message[$"MediaUrl{i}"],
                };
                attachments.Add(attachment);
            }

            return attachments;
        }

        /// <summary>
        /// Converts a query string to a dictionary with key-value pairs.
        /// </summary>
        /// <param name="query">The query string to convert.</param>
        /// <returns>A dictionary with the query values.</returns>
        private static Dictionary<string, string> QueryStringToDictionary(string query)
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
    }
}
