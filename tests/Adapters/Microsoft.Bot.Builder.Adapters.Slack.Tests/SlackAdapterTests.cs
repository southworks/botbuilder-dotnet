﻿// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Moq;
using SlackAPI;
using Xunit;

namespace Microsoft.Bot.Builder.Adapters.Slack.Tests
{
    public class SlackAdapterTests
    {
        [Fact]
        public void ConstructorShouldFailWithNullOptions()
        {
            var slackApi = new Mock<SlackClientWrapper>("BotToken");

            Assert.Throws<ArgumentNullException>(() => new SlackAdapter(slackApi.Object, null));
        }

        [Fact]
        public void ConstructorShouldFailWithNullSecurityMechanisms()
        {
            var slackAdapterOptions = new SlackAdapterOptions()
            {
                VerificationToken = null,
                ClientSigningSecret = null,
            };

            var slackApi = new Mock<SlackClientWrapper>("BotToken");

            Assert.Throws<Exception>(() => new SlackAdapter(slackApi.Object, slackAdapterOptions));
        }

        [Fact]
        public void ConstructorSucceeds()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "VerificationToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object.BotToken);

            // TODO: delete when LoginWithSlack method gets removed from SlackAdapter.
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            Assert.NotNull(new SlackAdapter(slackApi.Object, options.Object));
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldFailWithNullActivityId()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "VerificationToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object.BotToken);

            // TODO: delete when LoginWithSlack method gets removed from SlackAdapter.
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object, options.Object);

            var activity = new Activity
            {
                Id = null,
            };

            var turnContext = new TurnContext(slackAdapter, activity);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await slackAdapter.UpdateActivityAsync(turnContext, activity, default);
            });
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldFailWithNullActivityConversation()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "VerificationToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object.BotToken);

            // TODO: delete when LoginWithSlack method gets removed from SlackAdapter.
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));

            var slackAdapter = new SlackAdapter(slackApi.Object, options.Object);

            var activity = new Activity
            {
                Id = "testId",
                Conversation = null,
            };

            var turnContext = new TurnContext(slackAdapter, activity);

            await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await slackAdapter.UpdateActivityAsync(turnContext, activity, default);
            });
        }

        [Fact]
        public async Task UpdateActivityAsyncShouldSucceed()
        {
            var options = new Mock<SlackAdapterOptions>();
            options.Object.VerificationToken = "VerificationToken";
            options.Object.ClientSigningSecret = "ClientSigningSecret";
            options.Object.BotToken = "BotToken";

            var slackApi = new Mock<SlackClientWrapper>(options.Object.BotToken);

            // TODO: delete when LoginWithSlack method gets removed from SlackAdapter.
            slackApi.Setup(x => x.TestAuthAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult("mockedUserId"));
            slackApi.Setup(x => x.UpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), null, It.IsAny<bool>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(new UpdateResponse { ok = true }));

            var slackAdapter = new SlackAdapter(slackApi.Object, options.Object);

            var activity = new Mock<Activity>().SetupAllProperties();
            activity.Object.Id = "MockActivityId";
            activity.Object.Conversation = new ConversationAccount
            {
                Id = "MockConversationId",
            };
            activity.Object.Text = "Hello, Bot!";

            var turnContext = new TurnContext(slackAdapter, activity.Object);

            var response = await slackAdapter.UpdateActivityAsync(turnContext, activity.Object, default);

            Assert.Equal(activity.Object.Id, response.Id);
        }
    }
}
