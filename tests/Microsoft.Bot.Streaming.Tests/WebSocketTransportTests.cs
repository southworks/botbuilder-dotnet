﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Streaming;
using Microsoft.Bot.Streaming.Transport.WebSockets;
using Microsoft.Bot.Streaming.UnitTests.Mocks;
using Xunit;

namespace Microsoft.Bot.Streaming.UnitTests
{
    public class WebSocketTransportTests
    {
        [Fact]
        public async Task WebSocketServer_Connects()
        {
            var sock = new FauxSock();
            var writer = new WebSocketServer(sock, new StreamingRequestHandler(new MockBot(), new BotFrameworkHttpAdapter(), sock));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            writer.StartAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Assert.True(writer.IsConnected);
        }

        [Fact]
        public async Task WebSocketClient_ThrowsOnEmptyUrl()
        {
            Exception result = null;

            try
            {
                var reader = new WebSocketClient(string.Empty);
            }
            catch (Exception ex)
            {
                result = ex;
            }

            Assert.IsType<ArgumentNullException>(result);
        }

        [Fact]
        public async Task WebSocketClient_AcceptsAnyUrl()
        {
            Exception result = null;
            WebSocketClient reader = null;
            try
            {
                var webSocketClient = new WebSocketClient("fakeurl");
                reader = webSocketClient;
            }
            catch (Exception ex)
            {
                result = ex;
            }

            reader.Dispose();
            Assert.Null(result);
        }

        [Fact]
        public async Task WebSocketClient_ConnectThrowsIfUrlIsBad()
        {
            Exception result = null;
            WebSocketClient reader = null;
            IDictionary<string, string> fakeHeaders = new Dictionary<string, string>();
            fakeHeaders.Add("authorization", "totally");
            fakeHeaders.Add("channelId", "mtv");
            try
            {
                var webSocketClient = new WebSocketClient("fakeurl");
                reader = webSocketClient;
                await reader.ConnectAsync(fakeHeaders);
            }
            catch (Exception ex)
            {
                result = ex;
            }

            reader.Dispose();
            Assert.IsType<UriFormatException>(result);
        }

        [Fact]
        public async Task WebSocketTransport_Connects()
        {
            var sock = new FauxSock();
            sock.RealState = WebSocketState.Open;
            var transport = new WebSocketTransport(sock);

            Assert.True(transport.IsConnected);

            transport.Close();
            transport.Dispose();
        }

        [Fact]
        public async Task WebSocketTransport_SetsState()
        {
            var sock = new FauxSock();
            sock.RealState = WebSocketState.Open;
            var transport = new WebSocketTransport(sock);

            transport.Close();
            transport.Dispose();

            Assert.Equal(WebSocketState.Closed, sock.RealState);
        }

        [Fact]
        public async Task WebSocketTransport_CanSend()
        {
            // Arrange
            var sock = new FauxSock();
            sock.RealState = WebSocketState.Open;
            var transport = new WebSocketTransport(sock);
            var messageText = "This is a message.";
            byte[] message = Encoding.ASCII.GetBytes(messageText);

            // Act
            await transport.SendAsync(message, 0, message.Length);

            // Assert
            Assert.Equal(messageText, Encoding.UTF8.GetString(sock.SentArray));
        }

        [Fact]
        public async Task WebSocketTransport_CanReceive()
        {
            // Arrange
            var sock = new FauxSock();
            sock.RealState = WebSocketState.Open;
            var transport = new WebSocketTransport(sock);
            byte[] message = Encoding.ASCII.GetBytes("This is a message.");

            // Act
            var received = await transport.ReceiveAsync(message, 0, message.Length);

            // Assert
            Assert.Equal(message.Length, received);
        }

        private class FauxSock : WebSocket
        {
            public ArraySegment<byte> SentArray { get; set; }

            public ArraySegment<byte> ReceivedArray { get; set; }

            public WebSocketState RealState { get; set; }

            public override WebSocketCloseStatus? CloseStatus => throw new NotImplementedException();

            public override string CloseStatusDescription => throw new NotImplementedException();

            public override WebSocketState State { get => RealState; }

            public override string SubProtocol => throw new NotImplementedException();

            public override void Abort()
            {
                throw new NotImplementedException();
            }

            public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                RealState = WebSocketState.Closed;

                return Task.CompletedTask;
            }

            public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }

            public override void Dispose()
            {
                RealState = WebSocketState.Closed;

                return;
            }

            public override async Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
            {
                ReceivedArray = buffer;

                return new WebSocketReceiveResult(buffer.Count, WebSocketMessageType.Close, true);
            }

            public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
            {
                SentArray = buffer;

                return Task.CompletedTask;
            }
        }
    }
}
