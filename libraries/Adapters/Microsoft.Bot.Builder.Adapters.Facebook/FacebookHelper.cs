﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Adapters.Facebook
{
    public static class FacebookHelper
    {
        /// <summary>
        /// Converts an Activity object to a Facebook messenger outbound message ready for the API.
        /// </summary>
        /// <param name="activity">The activity to be converted to Facebook message.</param>
        /// <returns>The resulting message.</returns>
        public static FacebookMessage ActivityToFacebook(Activity activity)
        {
            if (activity == null)
            {
                return null;
            }

            var facebookMessage = new FacebookMessage(activity.Conversation.Id, new Message(), "RESPONSE");

            facebookMessage.Message.Text = activity.Text;

            // map these fields to their appropriate place
            if (activity.ChannelData != null)
            {
                facebookMessage.MessagingType = (activity.ChannelData as dynamic).messaging_type ?? null;

                facebookMessage.Tag = (activity.ChannelData as dynamic).tag ?? null;

                facebookMessage.Message.StickerId = (activity.ChannelData as dynamic).sticker_id ?? null;

                facebookMessage.Message.Attachment = (activity.ChannelData as dynamic).attachment ?? null;

                facebookMessage.PersonaId = (activity.ChannelData as dynamic).persona_id ?? null;

                facebookMessage.NotificationType = (activity.ChannelData as dynamic).notification_type ?? null;

                facebookMessage.SenderAction = (activity.ChannelData as dynamic).sender_action ?? null;

                // make sure the quick reply has a type
                if ((activity.ChannelData as dynamic).quick_replies != null)
                {
                    facebookMessage.Message.QuickReplies = (activity.ChannelData as dynamic).quick_replies; // TODO: Add the content_type depending of what shape quick_replies has
                }
            }

            return facebookMessage;
        }

        /// <summary>
        /// Handles each individual message inside a webhook payload (webhook may deliver more than one message at a time).
        /// </summary>
        /// <param name="message">The message to be processed.</param>
        /// <returns>An Activity with the result.</returns>
        public static Activity ProcessSingleMessage(FacebookMessage message)
        {
            if (message == null)
            {
                return null;
            }

            if (message.SenderId == null)
            {
                message.SenderId = (message as dynamic).optin?.user_ref;
            }

            var activity = new Activity()
            {
                ChannelId = "facebook",
                Timestamp = new DateTime(),
                Conversation = new ConversationAccount()
                {
                    Id = message.SenderId,
                },
                From = new ChannelAccount()
                {
                    Id = message.SenderId,
                    Name = message.SenderId,
                },
                Recipient = new ChannelAccount()
                {
                    Id = message.RecipientId,
                    Name = message.RecipientId,
                },
                ChannelData = message,
                Type = ActivityTypes.Event,
                Text = null,
            };

            if (message.Message != null)
            {
                activity.Type = ActivityTypes.Message;
                activity.Text = message.Message.Text;

                if ((activity.ChannelData as dynamic).message.is_echo)
                {
                    activity.Type = ActivityTypes.Event;
                }

                // copy fields like attachments, sticker, quick_reply, nlp, etc. // TODO Check
                activity.ChannelData = message.Message;
            }
            else if ((message as dynamic).postback != null)
            {
                activity.Type = ActivityTypes.Message;
                activity.Text = (message as dynamic).postback.payload;
            }

            return activity;
        }
    }
}
