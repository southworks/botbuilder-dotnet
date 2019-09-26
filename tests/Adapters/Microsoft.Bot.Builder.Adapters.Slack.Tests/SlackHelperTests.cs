﻿// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Schema;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Bot.Builder.Adapters.Slack.Tests
{
    public class SlackHelperTests
    {
        public const string ImageUrl = "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQtB3AwMUeNoq4gUBGe6Ocj8kyh3bXa9ZbV7u1fVKQoyKFHdkqU";

        [Fact]
        public void ActivityToSlackShouldReturnNullWithNullActivity()
        {
            Assert.Null(SlackHelper.ActivityToSlack(null));
        }

        [Fact]
        public void ActivityToSlackShouldReturnMessage()
        {
            var activity = new Activity
            {
                Timestamp = new DateTimeOffset(),
                Text = "Hello!",
                Attachments = new List<Attachment>
                {
                    new Attachment(name: "image", thumbnailUrl: ImageUrl),
                },
                Conversation = new ConversationAccount(id: "testId"),
            };

            var message = SlackHelper.ActivityToSlack(activity);

            Assert.Equal(activity.Conversation.Id, message.Channel);
            Assert.Equal(activity.Attachments[0].Name, message.Attachments[0].author_name);
        }

        [Fact]
        public void ActivityToSlackShouldReturnMessageFromChannelData()
        {
            var messageText = "Hello from message";

            var activity = new Activity
            {
                Timestamp = new DateTimeOffset(),
                Text = "Hello!",
                Recipient = new ChannelAccount("testRecipientId"),
                ChannelData = new NewSlackMessage
                {
                    Text = messageText,
                    Ephemeral = "testEphimeral",
                    IconUrl = new Uri(ImageUrl),
                },
                Conversation = new ConversationAccount(id: "testId"),
            };

            var message = SlackHelper.ActivityToSlack(activity);

            Assert.Equal(messageText, message.Text);
            Assert.False(message.AsUser);
        }

        [Fact]
        public void ActivityToSlackShouldReturnMessageWithThreadTS()
        {
            var serializeConversation = "{\"id\":\"testId\",\"thread_ts\":\"0001-01-01T00:00:00+00:00\"}";

            var activity = new Activity
            {
                Timestamp = new DateTimeOffset(),
                Text = "Hello!",
                Conversation = JsonConvert.DeserializeObject<ConversationAccount>(serializeConversation),
            };

            var message = SlackHelper.ActivityToSlack(activity);

            Assert.Equal(activity.Conversation.Id, message.Channel);
            Assert.Equal(activity.Conversation.Properties["thread_ts"], message.ThreadTS);
        }

        [Fact]
        public async Task WriteAsyncShouldFailWithNullResponse()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await SlackHelper.WriteAsync(null, HttpStatusCode.OK, "testText", Encoding.UTF8, new CancellationToken()).ConfigureAwait(false);
            });
        }

        [Fact]
        public async Task WriteAsyncShouldFailWithNullText()
        {
            var httpResponse = new Mock<HttpResponse>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await SlackHelper.WriteAsync(httpResponse.Object, HttpStatusCode.OK, null, Encoding.UTF8, new CancellationToken()).ConfigureAwait(false);
            });
        }

        [Fact]
        public async Task WriteAsyncShouldFailWithNullEncoding()
        {
            var httpResponse = new Mock<HttpResponse>();

            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await SlackHelper.WriteAsync(httpResponse.Object, HttpStatusCode.OK, "testText", null, new CancellationToken()).ConfigureAwait(false);
            });
        }

        [Fact]
        public async Task EventToActivityAsyncShouldReturnNull()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "TestToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object);

            var activity = await SlackHelper.EventToActivityAsync(null, slackApi.Object, new CancellationToken()).ConfigureAwait(false);

            Assert.Null(activity);
        }

        [Fact]
        public async Task EventToActivityAsyncShouldReturnActivity()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "TestToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"\Files\MessageBody.json");
            var slackBody = JsonConvert.DeserializeObject<SlackRequestBody>(payload);

            var activity = await SlackHelper.EventToActivityAsync(slackBody.Event, slackApi.Object, new CancellationToken()).ConfigureAwait(false);

            Assert.Equal(slackBody.Event.Text, activity.Text);
        }

        [Fact]
        public async Task EventToActivityAsyncShouldReturnActivityWithTeamId()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "TestToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"\Files\MessageBody.json");
            var slackBody = JsonConvert.DeserializeObject<SlackRequestBody>(payload);
            slackBody.Event.Channel = null;

            var activity = await SlackHelper.EventToActivityAsync(slackBody.Event, slackApi.Object, new CancellationToken()).ConfigureAwait(false);

            Assert.Equal(slackBody.Event.Team, activity.Conversation.Id);
        }

        [Fact]
        public async Task EventToActivityAsyncShouldReturnActivityWithItemChannel()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "TestToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"\Files\MessageBody.json");
            var slackBody = JsonConvert.DeserializeObject<SlackRequestBody>(payload);
            slackBody.Event.Channel = null;
            slackBody.Event.Item = "testItem";
            slackBody.Event.ItemChannel = "testItemChannel";

            var activity = await SlackHelper.EventToActivityAsync(slackBody.Event, slackApi.Object, new CancellationToken()).ConfigureAwait(false);

            Assert.Equal(slackBody.Event.ItemChannel, activity.Conversation.Id);
        }

        [Fact]
        public async Task CommandToActivityAsyncShouldReturnNull()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "TestToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object);

            var activity = await SlackHelper.CommandToActivityAsync(null, slackApi.Object, new CancellationToken()).ConfigureAwait(false);

            Assert.Null(activity);
        }

        [Fact]
        public async Task CommandToActivityAsyncShouldReturnActivity()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "TestToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object);

            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"\Files\SlashCommandBody.txt");
            var commandBody = SlackHelper.QueryStringToDictionary(payload);
            var slackBody = JsonConvert.DeserializeObject<SlackRequestBody>(JsonConvert.SerializeObject(commandBody));

            var activity = await SlackHelper.CommandToActivityAsync(slackBody, slackApi.Object, new CancellationToken()).ConfigureAwait(false);

            Assert.Equal(slackBody.TriggerId, activity.Id);
        }

        [Fact]
        public void PayloadToActivityShouldReturnNull()
        {
            var activity = SlackHelper.PayloadToActivity(null);

            Assert.Null(activity);
        }

        [Fact]
        public void QueryStringToDictionaryShouldReturnEmptyDictionary()
        {
            var dictionary = SlackHelper.QueryStringToDictionary(null);

            Assert.Empty(dictionary);
        }

        [Fact]
        public void QueryStringToDictionaryShouldReturnDictionary()
        {
            var payload = File.ReadAllText(Directory.GetCurrentDirectory() + @"\Files\SlashCommandBody.txt");

            var dictionary = SlackHelper.QueryStringToDictionary(payload);

            Assert.True(dictionary.Count > 0);
        }

        [Fact]
        public void DeserializeBodyShouldReturnNull()
        {
            var body = SlackHelper.DeserializeBody(null);

            Assert.Null(body);
        }
    }
}
