﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;

namespace Microsoft.Bot.Builder.AI.QnA.Tests
{
    [TestClass]
    public class QnAMakerTests
    {
        private const string _knowlegeBaseId = "dummy-id";
        private const string _endpointKey = "dummy-key";
        private const string _hostname = "https://dummy-hostname.azurewebsites.net/qnamaker";

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_TraceActivity()
        {
            // Mock Qna
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));
            var qna = GetQnAMaker(
                mockHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                },
                new QnAMakerOptions
                {
                    Top = 1,
                });

            // Invoke flow which uses mock
            var transcriptStore = new MemoryTranscriptStore();
            var adapter = new TestAdapter()
                .Use(new TranscriptLoggerMiddleware(transcriptStore));
            string conversationId = null;

            await new TestFlow(adapter, async (context, ct) =>
            {
                // Simulate Qna Lookup
                if (context?.Activity?.Text.CompareTo("how do I clean the stove?") == 0)
                {
                    var results = await qna.GetAnswersAsync(context);
                    Assert.IsNotNull(results);
                    Assert.AreEqual(results.Length, 1, "should get one result");
                    StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
                }

                conversationId = context.Activity.Conversation.Id;
                var typingActivity = new Activity
                {
                    Type = ActivityTypes.Typing,
                    RelatesTo = context.Activity.RelatesTo,
                };
                await context.SendActivityAsync(typingActivity);
                await Task.Delay(500);
                await context.SendActivityAsync("echo:" + context.Activity.Text);
            })
                .Send("how do I clean the stove?")
                    .AssertReply((activity) => Assert.AreEqual(activity.Type, ActivityTypes.Typing))
                    .AssertReply("echo:how do I clean the stove?")
                .Send("bar")
                    .AssertReply((activity) => Assert.AreEqual(activity.Type, ActivityTypes.Typing))
                    .AssertReply("echo:bar")
                .StartTestAsync();

            // Validate Trace Activity created
            var pagedResult = await transcriptStore.GetTranscriptActivitiesAsync("test", conversationId);
            Assert.AreEqual(7, pagedResult.Items.Length);
            Assert.AreEqual("how do I clean the stove?", pagedResult.Items[0].AsMessageActivity().Text);
            Assert.IsTrue(pagedResult.Items[1].Type.CompareTo(ActivityTypes.Trace) == 0);
            var traceInfo = ((JObject)((ITraceActivity)pagedResult.Items[1]).Value).ToObject<QnAMakerTraceInfo>();
            Assert.IsNotNull(traceInfo);
            Assert.IsNotNull(pagedResult.Items[2].AsTypingActivity());
            Assert.AreEqual("echo:how do I clean the stove?", pagedResult.Items[3].AsMessageActivity().Text);
            Assert.AreEqual("bar", pagedResult.Items[4].AsMessageActivity().Text);
            Assert.IsNotNull(pagedResult.Items[5].AsTypingActivity());
            Assert.AreEqual("echo:bar", pagedResult.Items[6].AsMessageActivity().Text);
            foreach (var activity in pagedResult.Items)
            {
                Assert.IsTrue(!string.IsNullOrWhiteSpace(activity.Id));
                Assert.IsTrue(activity.Timestamp > default(DateTimeOffset));
            }
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task QnaMaker_TraceActivity_EmptyText()
        {
            // Get basic Qna
            var qna = QnaReturnsAnswer();

            // No text
            var adapter = new TestAdapter();
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                Text = string.Empty,
                Conversation = new ConversationAccount(),
                Recipient = new ChannelAccount(),
                From = new ChannelAccount(),
            };
            var context = new TurnContext(adapter, activity);

            var results = await qna.GetAnswersAsync(context);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task QnaMaker_TraceActivity_NullText()
        {
            // Get basic Qna
            var qna = QnaReturnsAnswer();

            // No text
            var adapter = new TestAdapter();
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                Text = null,
                Conversation = new ConversationAccount(),
                Recipient = new ChannelAccount(),
                From = new ChannelAccount(),
            };
            var context = new TurnContext(adapter, activity);

            var results = await qna.GetAnswersAsync(context);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task QnaMaker_TraceActivity_NullContext()
        {
            // Get basic Qna
            var qna = QnaReturnsAnswer();

            var results = await qna.GetAnswersAsync(null);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentException))]
        public async Task QnaMaker_TraceActivity_BadMessage()
        {
            // Get basic Qna
            var qna = QnaReturnsAnswer();

            // No text
            var adapter = new TestAdapter();
            var activity = new Activity
            {
                Type = ActivityTypes.Trace,
                Text = "My Text",
                Conversation = new ConversationAccount(),
                Recipient = new ChannelAccount(),
                From = new ChannelAccount(),
            };
            var context = new TurnContext(adapter, activity);

            var results = await qna.GetAnswersAsync(context);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task QnaMaker_TraceActivity_NullActivity()
        {
            // Get basic Qna
            var qna = QnaReturnsAnswer();

            // No text
            var adapter = new TestAdapter();
            var context = new MyTurnContext(adapter, null);

            var results = await qna.GetAnswersAsync(context);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_ReturnsAnswer()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var qna = GetQnAMaker(
                mockHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                },
                new QnAMakerOptions
                {
                    Top = 1,
                });

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_ReturnsAnswer_Configuration()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var service = new QnAMakerService
            {
                KbId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Hostname = _hostname,
            };

            var options = new QnAMakerOptions
            {
                Top = 1,
            };

            var client = new HttpClient(mockHttp);
            var qna = new QnAMaker(service, options, client);

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_ReturnsAnswerWithFiltering()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_UsesStrictFilters_ToReturnAnswer.json"));

            var interceptHttp = new InterceptRequestHandler(mockHttp);

            var qna = GetQnAMaker(
                interceptHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                });

            var options = new QnAMakerOptions
            {
                StrictFilters = new Metadata[]
                {
                    new Metadata() { Name = "topic", Value = "value" },
                },
                Top = 1,
            };

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"), options);
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
            Assert.AreEqual("topic", results[0].Metadata[0].Name);
            Assert.AreEqual("value", results[0].Metadata[0].Value);

            // verify we are actually passing on the options
            var obj = JObject.Parse(interceptHttp.Content);
            Assert.AreEqual(1, obj["top"].Value<int>());
            Assert.AreEqual("topic", obj["strictFilters"][0]["name"].Value<string>());
            Assert.AreEqual("value", obj["strictFilters"][0]["value"].Value<string>());
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_SetScoreThresholdWhenThresholdIsZero()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var qnaWithZeroValueThreshold = GetQnAMaker(
                mockHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                },
                new QnAMakerOptions()
                {
                    ScoreThreshold = 0.0F,
                });

            var results = await qnaWithZeroValueThreshold
                .GetAnswersAsync(GetContext("how do I clean the stove?"), new QnAMakerOptions() { Top = 1 });

            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Length);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_TestThreshold()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_TestThreshold.json"));

            var qna = GetQnAMaker(
                mockHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                },
                new QnAMakerOptions
                {
                    Top = 1,
                    ScoreThreshold = 0.99F,
                });

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 0, "should get zero result because threshold");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void QnaMaker_Test_ScoreThresholdTooLarge_OutOfRange()
        {
            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };

            var tooLargeThreshold = new QnAMakerOptions
            {
                ScoreThreshold = 1.1F,
                Top = 1,
            };

            var qnaWithLargeThreshold = new QnAMaker(endpoint, tooLargeThreshold);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void QnaMaker_Test_ScoreThresholdTooSmall_OutOfRange()
        {
            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };

            var tooSmallThreshold = new QnAMakerOptions
            {
                ScoreThreshold = -9000.0F,
                Top = 1,
            };

            var qnaWithSmallThreshold = new QnAMaker(endpoint, tooSmallThreshold);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void QnaMaker_Test_Top_OutOfRange()
        {
            var qna = new QnAMaker(
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                },
                new QnAMakerOptions
                {
                    Top = -1,
                    ScoreThreshold = 0.5F,
                });
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentException))]
        public void QnaMaker_Test_Endpoint_EmptyKbId()
        {
            var qnaNullEndpoint = new QnAMaker(
                new QnAMakerEndpoint()
                {
                    KnowledgeBaseId = string.Empty,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                });
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentException))]
        public void QnaMaker_Test_Endpoint_EmptyEndpointKey()
        {
            var qnaNullEndpoint = new QnAMaker(
                new QnAMakerEndpoint()
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = string.Empty,
                    Host = _hostname,
                });
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(ArgumentException))]
        public void QnaMaker_Test_Endpoint_EmptyHost()
        {
            var qnaNullEndpoint = new QnAMaker(
                new QnAMakerEndpoint()
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = string.Empty,
                });
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_UserAgent()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var interceptHttp = new InterceptRequestHandler(mockHttp);

            var qna = GetQnAMaker(
                interceptHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                },
                new QnAMakerOptions
                {
                    Top = 1,
                });

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));

            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");

            // Verify that we added the bot.builder package details.
            Assert.IsTrue(interceptHttp.UserAgent.Contains("Microsoft.Bot.Builder.AI.QnA/4"));
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(NotSupportedException))]
        public async Task QnaMaker_V2LegacyEndpoint_ConvertsToHaveIdPropertyInResult()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetV2LegacyRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_LegacyEndpointAnswer.json"));

            var v2LegacyEndpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = $"{_hostname}/v2.0",
            };

            var v2Qna = GetQnAMaker(mockHttp, v2LegacyEndpoint);

            var v2legacyResult = await v2Qna.GetAnswersAsync(GetContext("How do I be the best?"));

            Assert.IsNotNull(v2legacyResult);
            Assert.IsTrue(v2legacyResult[0]?.Id != null);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_V3LegacyEndpoint_ConvertsToHaveIdPropertyInResult()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetV3LegacyRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_LegacyEndpointAnswer.json"));

            var v3LegacyEndpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = $"{_hostname}/v3.0",
            };

            var v3Qna = GetQnAMaker(mockHttp, v3LegacyEndpoint);

            var v3legacyResult = await v3Qna.GetAnswersAsync(GetContext("How do I be the best?"));

            Assert.IsNotNull(v3legacyResult);
            Assert.IsTrue(v3legacyResult[0]?.Id != null);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_ReturnsAnswerWithMetadataBoost()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswersWithMetadataBoost.json"));

            var interceptHttp = new InterceptRequestHandler(mockHttp);

            var qna = GetQnAMaker(
                interceptHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                });

            var options = new QnAMakerOptions
            {
                MetadataBoost = new Metadata[]
                {
                    new Metadata() { Name = "artist", Value = "drake" },
                },
                Top = 1,
            };

            var results = await qna.GetAnswersAsync(GetContext("who loves me?"), options);

            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "Kiki");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        public async Task QnaMaker_TestThresholdInQueryOption()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer_GivenScoreThresholdQueryOption.json"));

            var interceptHttp = new InterceptRequestHandler(mockHttp);

            var qna = GetQnAMaker(
                interceptHttp,
                new QnAMakerEndpoint()
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                });

            var queryOptionsWithScoreThreshold = new QnAMakerOptions()
            {
                ScoreThreshold = 0.5F,
                Top = 2,
            };

            var result = await qna.GetAnswersAsync(
                    GetContext("What happens when you hug a porcupine?"),
                    queryOptionsWithScoreThreshold);

            Assert.IsNotNull(result);

            var obj = JObject.Parse(interceptHttp.Content);
            Assert.AreEqual(2, obj["top"].Value<int>());
            Assert.AreEqual(0.5F, obj["scoreThreshold"].Value<float>());
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [ExpectedException(typeof(HttpRequestException))]
        public async Task QnaMaker_Test_UnsuccessfulResponse()
        {
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond(System.Net.HttpStatusCode.BadGateway);

            var qna = GetQnAMaker(
                mockHttp,
                new QnAMakerEndpoint()
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                });

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [TestCategory("Telemetry")]
        public async Task Telemetry_NullTelemetryClient()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var client = new HttpClient(mockHttp);

            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };
            var options = new QnAMakerOptions
            {
                Top = 1,
            };

            // Act (Null Telemetry client)
            //    This will default to the NullTelemetryClient which no-ops all calls.
            var qna = new QnAMaker(endpoint, options, client, null, true);
            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));

            // Assert - Validate we didn't break QnA functionality.
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
            StringAssert.StartsWith(results[0].Source, "Editorial");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [TestCategory("Telemetry")]
        public async Task Telemetry_ReturnsAnswer()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var client = new HttpClient(mockHttp);

            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };
            var options = new QnAMakerOptions
            {
                Top = 1,
            };
            var telemetryClient = new Mock<IBotTelemetryClient>();

            // Act - See if we get data back in telemetry
            var qna = new QnAMaker(endpoint, options, client, telemetryClient: telemetryClient.Object, logPersonalInformation: true);
            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));

            // Assert - Check Telemetry logged
            Assert.AreEqual(telemetryClient.Invocations.Count, 1);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments.Count, 3);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments[0], QnATelemetryConstants.QnaMsgEvent);
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("knowledgeBaseId"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("matchedQuestion"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("question"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("questionId"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("answer"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["answer"], "BaseCamp: You can use a damp rag to clean around the Power Pack");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("articleFound"));
            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).Count, 1);
            Assert.IsTrue(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).ContainsKey("score"));

            // Assert - Validate we didn't break QnA functionality.
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
            StringAssert.StartsWith(results[0].Source, "Editorial");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [TestCategory("Telemetry")]
        public async Task Telemetry_PII()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var client = new HttpClient(mockHttp);

            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };
            var options = new QnAMakerOptions
            {
                Top = 1,
            };
            var telemetryClient = new Mock<IBotTelemetryClient>();

            // Act
            var qna = new QnAMaker(endpoint, options, client, telemetryClient.Object, false);
            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"));

            // Assert - Validate PII properties not logged.
            Assert.AreEqual(telemetryClient.Invocations.Count, 1);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments.Count, 3);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments[0], QnATelemetryConstants.QnaMsgEvent);
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("knowledgeBaseId"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("matchedQuestion"));
            Assert.IsFalse(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("question"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("questionId"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("answer"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["answer"], "BaseCamp: You can use a damp rag to clean around the Power Pack");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("articleFound"));
            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).Count, 1);
            Assert.IsTrue(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).ContainsKey("score"));

            // Assert - Validate we didn't break QnA functionality.
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
            StringAssert.StartsWith(results[0].Source, "Editorial");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [TestCategory("Telemetry")]
        public async Task Telemetry_Override()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var client = new HttpClient(mockHttp);

            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };
            var options = new QnAMakerOptions
            {
                Top = 1,
            };
            var telemetryClient = new Mock<IBotTelemetryClient>();

            // Act - Override the QnaMaker object to log custom stuff and honor parms passed in.
            var telemetryProperties = new Dictionary<string, string>
            {
                { "Id", "MyID" },
            };
            var qna = new OverrideTelemetry(endpoint, options, client, telemetryClient.Object, false);
            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"), null, telemetryProperties);

            // Assert
            Assert.AreEqual(telemetryClient.Invocations.Count, 2);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments.Count, 3);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments[0], QnATelemetryConstants.QnaMsgEvent);
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).Count == 2);
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("MyImportantProperty"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["MyImportantProperty"], "myImportantValue");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("Id"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["Id"], "MyID");

            Assert.AreEqual(telemetryClient.Invocations[1].Arguments[0], "MySecondEvent");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[1].Arguments[1]).ContainsKey("MyImportantProperty2"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[1].Arguments[1])["MyImportantProperty2"], "myImportantValue2");

            // Validate we didn't break QnA functionality.
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
            StringAssert.StartsWith(results[0].Source, "Editorial");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [TestCategory("Telemetry")]
        public async Task Telemetry_AdditionalPropsMetrics()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var client = new HttpClient(mockHttp);

            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };
            var options = new QnAMakerOptions
            {
                Top = 1,
            };
            var telemetryClient = new Mock<IBotTelemetryClient>();

            // Act - Pass in properties during QnA invocation
            var qna = new QnAMaker(endpoint, options, client, telemetryClient.Object, false);
            var telemetryProperties = new Dictionary<string, string>
            {
                { "MyImportantProperty", "myImportantValue" },
            };
            var telemetryMetrics = new Dictionary<string, double>
            {
                { "MyImportantMetric", 3.14159 },
            };

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"), null, telemetryProperties, telemetryMetrics);

            // Assert - added properties were added.
            Assert.AreEqual(telemetryClient.Invocations.Count, 1);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments.Count, 3);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments[0], QnATelemetryConstants.QnaMsgEvent);
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey(QnATelemetryConstants.KnowledgeBaseIdProperty));
            Assert.IsFalse(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey(QnATelemetryConstants.QuestionProperty));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey(QnATelemetryConstants.MatchedQuestionProperty));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey(QnATelemetryConstants.QuestionIdProperty));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey(QnATelemetryConstants.AnswerProperty));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["answer"], "BaseCamp: You can use a damp rag to clean around the Power Pack");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("MyImportantProperty"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["MyImportantProperty"], "myImportantValue");

            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).Count, 2);
            Assert.IsTrue(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).ContainsKey("score"));
            Assert.IsTrue(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).ContainsKey("MyImportantMetric"));
            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2])["MyImportantMetric"], 3.14159);

            // Validate we didn't break QnA functionality.
            Assert.IsNotNull(results);
            Assert.AreEqual(results.Length, 1, "should get one result");
            StringAssert.StartsWith(results[0].Answer, "BaseCamp: You can use a damp rag to clean around the Power Pack");
            StringAssert.StartsWith(results[0].Source, "Editorial");
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [TestCategory("Telemetry")]
        public async Task Telemetry_AdditionalPropsOverride()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var client = new HttpClient(mockHttp);

            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };
            var options = new QnAMakerOptions
            {
                Top = 1,
            };
            var telemetryClient = new Mock<IBotTelemetryClient>();

            // Act - Pass in properties during QnA invocation that override default properties
            //  NOTE: We are invoking this with PII turned OFF, and passing a PII property (originalQuestion).
            var qna = new QnAMaker(endpoint, options, client, telemetryClient.Object, false);
            var telemetryProperties = new Dictionary<string, string>
            {
                { "knowledgeBaseId", "myImportantValue" },
                { "originalQuestion", "myImportantValue2" },
            };
            var telemetryMetrics = new Dictionary<string, double>
            {
                { "score", 3.14159 },
            };

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"), null, telemetryProperties, telemetryMetrics);

            // Assert - added properties were added.
            Assert.AreEqual(telemetryClient.Invocations.Count, 1);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments.Count, 3);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments[0], QnATelemetryConstants.QnaMsgEvent);
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("knowledgeBaseId"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["knowledgeBaseId"], "myImportantValue");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("matchedQuestion"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["originalQuestion"], "myImportantValue2");
            Assert.IsFalse(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("question"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("questionId"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("answer"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["answer"], "BaseCamp: You can use a damp rag to clean around the Power Pack");
            Assert.IsFalse(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("MyImportantProperty"));

            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).Count, 1);
            Assert.IsTrue(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).ContainsKey("score"));
            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2])["score"], 3.14159);
        }

        [TestMethod]
        [TestCategory("AI")]
        [TestCategory("QnAMaker")]
        [TestCategory("Telemetry")]
        public async Task Telemetry_FillPropsOverride()
        {
            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));

            var client = new HttpClient(mockHttp);

            var endpoint = new QnAMakerEndpoint
            {
                KnowledgeBaseId = _knowlegeBaseId,
                EndpointKey = _endpointKey,
                Host = _hostname,
            };
            var options = new QnAMakerOptions
            {
                Top = 1,
            };
            var telemetryClient = new Mock<IBotTelemetryClient>();

            // Act - Pass in properties during QnA invocation that override default properties
            //       In addition Override with derivation.  This presents an interesting question of order of setting properties.
            //       If I want to override "originalQuestion" property:
            //           - Set in "Stock" schema
            //           - Set in derived QnAMaker class
            //           - Set in GetAnswersAsync
            //       Logically, the GetAnswersAync should win.  But ultimately OnQnaResultsAsync decides since it is the last
            //       code to touch the properties before logging (since it actually logs the event).
            var qna = new OverrideFillTelemetry(endpoint, options, client, telemetryClient.Object, false);
            var telemetryProperties = new Dictionary<string, string>
            {
                { "knowledgeBaseId", "myImportantValue" },
                { "matchedQuestion", "myImportantValue2" },
            };
            var telemetryMetrics = new Dictionary<string, double>
            {
                { "score", 3.14159 },
            };

            var results = await qna.GetAnswersAsync(GetContext("how do I clean the stove?"), null, telemetryProperties, telemetryMetrics);

            // Assert - added properties were added.
            Assert.AreEqual(telemetryClient.Invocations.Count, 2);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments.Count, 3);
            Assert.AreEqual(telemetryClient.Invocations[0].Arguments[0], QnATelemetryConstants.QnaMsgEvent);
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).Count, 6);
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("knowledgeBaseId"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["knowledgeBaseId"], "myImportantValue");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("matchedQuestion"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["matchedQuestion"], "myImportantValue2");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("questionId"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("answer"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["answer"], "BaseCamp: You can use a damp rag to clean around the Power Pack");
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("articleFound"));
            Assert.IsTrue(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1]).ContainsKey("MyImportantProperty"));
            Assert.AreEqual(((Dictionary<string, string>)telemetryClient.Invocations[0].Arguments[1])["MyImportantProperty"], "myImportantValue");

            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).Count, 1);
            Assert.IsTrue(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2]).ContainsKey("score"));
            Assert.AreEqual(((Dictionary<string, double>)telemetryClient.Invocations[0].Arguments[2])["score"], 3.14159);
        }

        private static TurnContext GetContext(string utterance)
        {
            var b = new TestAdapter();
            var a = new Activity
            {
                Type = ActivityTypes.Message,
                Text = utterance,
                Conversation = new ConversationAccount(),
                Recipient = new ChannelAccount(),
                From = new ChannelAccount(),
            };
            return new TurnContext(b, a);
        }

        private string GetV2LegacyRequestUrl() => $"{_hostname}/v2.0/knowledgebases/{_knowlegeBaseId}/generateanswer";

        private string GetV3LegacyRequestUrl() => $"{_hostname}/v3.0/knowledgebases/{_knowlegeBaseId}/generateanswer";

        private string GetRequestUrl() => $"{_hostname}/knowledgebases/{_knowlegeBaseId}/generateanswer";

        private Stream GetResponse(string fileName)
        {
            var path = Path.Combine(Environment.CurrentDirectory, "TestData", fileName);
            return File.OpenRead(path);
        }

        /// <summary>
        /// Return a stock Mocked Qna thats loaded with QnaMaker_ReturnsAnswer.json
        /// Used for tests that just require any old qna instance.
        /// </summary>
        /// <returns>
        /// QnAMaker.
        /// </returns>
        private QnAMaker QnaReturnsAnswer()
        {
            // Mock Qna
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(HttpMethod.Post, GetRequestUrl())
                    .Respond("application/json", GetResponse("QnaMaker_ReturnsAnswer.json"));
            var qna = GetQnAMaker(
                mockHttp,
                new QnAMakerEndpoint
                {
                    KnowledgeBaseId = _knowlegeBaseId,
                    EndpointKey = _endpointKey,
                    Host = _hostname,
                },
                new QnAMakerOptions
                {
                    Top = 1,
                });
            return qna;
        }

        private QnAMaker GetQnAMaker(HttpMessageHandler messageHandler, QnAMakerEndpoint endpoint, QnAMakerOptions options = null)
        {
            var client = new HttpClient(messageHandler);
            return new QnAMaker(endpoint, options, client);
        }

        public class OverrideTelemetry : QnAMaker
        {
            public OverrideTelemetry(QnAMakerEndpoint endpoint, QnAMakerOptions options, HttpClient httpClient, IBotTelemetryClient telemetryClient, bool logPersonalInformation)
                : base(endpoint, options, httpClient, telemetryClient, logPersonalInformation)
            {
            }

            protected override Task OnQnaResultsAsync(
                                        QueryResult[] queryResults,
                                        ITurnContext turnContext,
                                        Dictionary<string, string> telemetryProperties = null,
                                        Dictionary<string, double> telemetryMetrics = null,
                                        CancellationToken cancellationToken = default(CancellationToken))
            {
                var properties = telemetryProperties ?? new Dictionary<string, string>();

                // GetAnswerAsync overrides derived class.
                properties.TryAdd("MyImportantProperty", "myImportantValue");

                // Log event
                TelemetryClient.TrackEvent(
                                QnATelemetryConstants.QnaMsgEvent,
                                properties);

                // Create second event.
                var secondEventProperties = new Dictionary<string, string>();
                secondEventProperties.Add("MyImportantProperty2", "myImportantValue2");
                TelemetryClient.TrackEvent(
                                "MySecondEvent",
                                secondEventProperties);
                return Task.CompletedTask;
            }
        }

        public class OverrideFillTelemetry : QnAMaker
        {
            public OverrideFillTelemetry(QnAMakerEndpoint endpoint, QnAMakerOptions options, HttpClient httpClient, IBotTelemetryClient telemetryClient, bool logPersonalInformation)
                : base(endpoint, options, httpClient, telemetryClient, logPersonalInformation)
            {
            }

            protected override async Task OnQnaResultsAsync(
                                        QueryResult[] queryResults,
                                        ITurnContext turnContext,
                                        Dictionary<string, string> telemetryProperties = null,
                                        Dictionary<string, double> telemetryMetrics = null,
                                        CancellationToken cancellationToken = default(CancellationToken))
            {
                var eventData = await FillQnAEventAsync(queryResults, turnContext, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);

                // Add my property
                eventData.Properties.Add("MyImportantProperty", "myImportantValue");

                // Log QnaMessage event
                TelemetryClient.TrackEvent(
                                QnATelemetryConstants.QnaMsgEvent,
                                eventData.Properties,
                                eventData.Metrics);

                // Create second event.
                var secondEventProperties = new Dictionary<string, string>();
                secondEventProperties.Add("MyImportantProperty2", "myImportantValue2");
                TelemetryClient.TrackEvent(
                                "MySecondEvent",
                                secondEventProperties);
            }
        }
    }
}
