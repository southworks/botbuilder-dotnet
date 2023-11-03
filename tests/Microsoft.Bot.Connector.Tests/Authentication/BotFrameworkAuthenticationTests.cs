// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Bot.Connector.Tests.Authentication
{
    public class BotFrameworkAuthenticationTests
    {
        [Fact]
        public async Task CreatesBotFrameworkClient()
        {
            // Arrange
            string fromBotId = "from-bot-id";
            string toBotId = "to-bot-id";
            string loginUrl = AuthenticationConstants.ToChannelFromBotLoginUrlTemplate;
            Uri toUrl = new Uri("http://test1.com/test");

            var credentialFactoryMock = new Mock<ServiceClientCredentialsFactory>();
            credentialFactoryMock.Setup(cssf => cssf.CreateCredentialsAsync(
                It.Is<string>(v => v == fromBotId),
                It.Is<string>(v => v == toBotId),
                It.Is<string>(v => v == loginUrl),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(MicrosoftAppCredentials.Empty);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Content = new StringContent("{ \"hello\": \"world\" }");

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(hrm => hrm.RequestUri == toUrl), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(hcf => hcf.CreateClient(It.IsAny<string>())).Returns(client);

            var bfa = BotFrameworkAuthenticationFactory.Create(
                channelService: null,
                validateAuthority: true,
                toChannelFromBotLoginUrl: null,
                toChannelFromBotOAuthScope: null,
                toBotFromChannelTokenIssuer: null,
                oAuthUrl: null,
                toBotFromChannelOpenIdMetadataUrl: null,
                toBotFromEmulatorOpenIdMetadataUrl: null,
                callerId: null,
                credentialFactoryMock.Object,
                new AuthenticationConfiguration(),
                httpClientFactoryMock.Object,
                NullLogger.Instance);

            Uri serviceUrl = new Uri("http://root-bot/service-url");
            string conversationId = "conversation-id";
            var activity = new Activity
            {
                ChannelId = "channel-id",
                ServiceUrl = "service-url",
                Locale = "locale",
                Conversation = new ConversationAccount
                {
                    Id = "conversationiid",
                    Name = "conversation-name"
                }
            };

            // Act
            var bfc = bfa.CreateBotFrameworkClient();
            var invokeResponse = await bfc.PostActivityAsync(fromBotId, toBotId, toUrl, serviceUrl, conversationId, activity);

            // Assert
            Assert.Equal("world", ((JObject)invokeResponse.Body)["hello"].ToString());
        }

        [Fact]
        public async Task ConnectorClientWithCustomOptions()
        {
            // Arrange
            string fromBotId = "from-bot-id";
            string toBotId = "to-bot-id";
            string loginUrl = AuthenticationConstants.ToChannelFromBotLoginUrlTemplate;
            Uri toUrl = new Uri("http://test1.com/test");

            var credentialFactoryMock = new Mock<ServiceClientCredentialsFactory>();
            credentialFactoryMock.Setup(cssf => cssf.CreateCredentialsAsync(
                It.Is<string>(v => v == fromBotId),
                It.Is<string>(v => v == toBotId),
                It.Is<string>(v => v == loginUrl),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(MicrosoftAppCredentials.Empty);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Content = new StringContent("{ \"hello\": \"world\" }");

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(hrm => hrm.RequestUri == toUrl), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(hcf => hcf.CreateClient(It.IsAny<string>())).Returns(client);

            var userAgent = $"CustomUserAgent/{Guid.NewGuid()}";
            var bfa = BotFrameworkAuthenticationFactory.Create(
                channelService: null,
                validateAuthority: true,
                toChannelFromBotLoginUrl: null,
                toChannelFromBotOAuthScope: null,
                toBotFromChannelTokenIssuer: null,
                oAuthUrl: null,
                toBotFromChannelOpenIdMetadataUrl: null,
                toBotFromEmulatorOpenIdMetadataUrl: null,
                callerId: null,
                credentialFactoryMock.Object,
                new AuthenticationConfiguration(),
                httpClientFactoryMock.Object,
                NullLogger.Instance,
                new ConnectorClientOptions { UserAgent = userAgent });

            // Act
            var bfc = bfa.CreateConnectorFactory(new Mock<ClaimsIdentity>().Object);
            await bfc.CreateAsync("http://root-bot/service-url", "audience", CancellationToken.None);

            // Assert
            Assert.Contains(userAgent, httpClientFactoryMock.Object.CreateClient().DefaultRequestHeaders.UserAgent.ToString());
        }

        [Fact]
        public async Task UserTokenClientWithCustomOptions()
        {
            // Arrange
            string fromBotId = "from-bot-id";
            string toBotId = AuthenticationConstants.ToChannelFromBotOAuthScope;
            string loginUrl = AuthenticationConstants.ToChannelFromBotLoginUrlTemplate;
            Uri toUrl = new Uri("http://test1.com/test");

            var credentialFactoryMock = new Mock<ServiceClientCredentialsFactory>();
            credentialFactoryMock.Setup(cssf => cssf.CreateCredentialsAsync(
                It.Is<string>(v => v == fromBotId),
                It.Is<string>(v => v == toBotId),
                It.Is<string>(v => v == loginUrl),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>())).ReturnsAsync(MicrosoftAppCredentials.Empty);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            responseMessage.Content = new StringContent("{ \"hello\": \"world\" }");

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(hrm => hrm.RequestUri == toUrl), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(responseMessage);

            var client = new HttpClient(mockHttpMessageHandler.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(hcf => hcf.CreateClient(It.IsAny<string>())).Returns(client);

            var userAgent = $"CustomUserAgent/{Guid.NewGuid()}";
            var bfa = BotFrameworkAuthenticationFactory.Create(
                channelService: null,
                validateAuthority: true,
                toChannelFromBotLoginUrl: null,
                toChannelFromBotOAuthScope: null,
                toBotFromChannelTokenIssuer: null,
                oAuthUrl: null,
                toBotFromChannelOpenIdMetadataUrl: null,
                toBotFromEmulatorOpenIdMetadataUrl: null,
                callerId: null,
                credentialFactoryMock.Object,
                new AuthenticationConfiguration(),
                httpClientFactoryMock.Object,
                NullLogger.Instance,
                new ConnectorClientOptions { UserAgent = userAgent });

            var claims = new ClaimsIdentity();
            claims.AddClaim(new Claim(AuthenticationConstants.AudienceClaim, fromBotId));

            // Act
            await bfa.CreateUserTokenClientAsync(claims, CancellationToken.None);

            // Assert
            Assert.Contains(userAgent, httpClientFactoryMock.Object.CreateClient().DefaultRequestHeaders.UserAgent.ToString());
        }
    }
}
