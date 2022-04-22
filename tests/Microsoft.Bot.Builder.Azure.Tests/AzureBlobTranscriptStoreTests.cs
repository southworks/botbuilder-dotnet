// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Bot.Schema;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.Bot.Builder.Azure.Tests
{
    public class AzureBlobTranscriptStoreTests
    {
        protected const string ConnectionString = @"UseDevelopmentStorage=true";
        protected const string ContainerName = "containername";

        [Fact]
        public void ConstructorsValidation()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            Assert.NotNull(new AzureBlobTranscriptStore(ConnectionString, ContainerName));
            Assert.NotNull(new AzureBlobTranscriptStore(storageAccount, ContainerName));

            // No storageAccount. Should throw.
            Assert.Throws<ArgumentNullException>(() => new AzureBlobTranscriptStore(storageAccount: null, ContainerName));

            // No containerName. Should throw.
            Assert.Throws<ArgumentNullException>(() => new AzureBlobTranscriptStore(storageAccount, null));

            Assert.Throws<FormatException>(() => new AzureBlobTranscriptStore("123", ContainerName));
        }

        [Fact]
        public async Task LogActivityAsyncNullActivityFailure()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName);

            await Assert.ThrowsAsync<ArgumentNullException>(() => blobTranscript.LogActivityAsync(null));
        }

        [Fact]
        public async Task LogActivityAsyncDefault()
        {
            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Setup(x => x.OpenWriteAsync()).Returns(Task.FromResult(stream.Object));
            mockBlockBlob.Setup(x => x.SetMetadataAsync());

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetBlockBlobReference(It.IsAny<string>())).Returns(mockBlockBlob.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var mockAccount = new Mock<CloudStorageAccount>(new StorageCredentials("accountName", "S2V5VmFsdWU=", "key"), false);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);
            var activity = new Activity
            {
                Type = ActivityTypes.Message,
                Text = "Hello",
                Id = "test-id",
                ChannelId = "channel-id",
                Conversation = new ConversationAccount() { Id = "convo-id" },
                Timestamp = new DateTimeOffset(),
                From = new ChannelAccount() { Id = "account-1" },
                Recipient = new ChannelAccount() { Id = "account-2" }
            };

            await blobTranscript.LogActivityAsync(activity);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetBlockBlobReference(It.IsAny<string>()), Times.Once);
            mockBlockBlob.Verify(x => x.OpenWriteAsync(), Times.Once);
            mockBlockBlob.Verify(x => x.SetMetadataAsync(), Times.Once);
        }

        [Fact]
        public async Task LogActivityAsyncMessageUpdate()
        {
            var activity = new Activity
            {
                Type = ActivityTypes.MessageUpdate,
                Text = "Hello",
                Id = "test-id",
                ChannelId = "channel-id",
                Conversation = new ConversationAccount { Id = "convo-id" },
                Timestamp = new DateTimeOffset(),
                From = new ChannelAccount { Id = "account-1" },
                Recipient = new ChannelAccount { Id = "account-2" }
            };

            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Setup(x => x.DownloadTextAsync()).Returns(Task.FromResult(JsonConvert.SerializeObject(activity)));
            mockBlockBlob.Setup(x => x.OpenWriteAsync()).Returns(Task.FromResult(stream.Object));
            mockBlockBlob.Setup(x => x.SetMetadataAsync());

            var segment = new BlobResultSegment(new List<CloudBlockBlob> { mockBlockBlob.Object }, null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                It.IsAny<int>(),
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.LogActivityAsync(activity);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockBlockBlob.Verify(x => x.DownloadTextAsync(), Times.Exactly(2));
            mockBlockBlob.Verify(x => x.OpenWriteAsync(), Times.Once);
            mockBlockBlob.Verify(x => x.SetMetadataAsync(), Times.Exactly(2));
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    It.IsAny<int>(),
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);
        }

        [Fact]
        public async Task LogActivityAsyncMessageUpdateNullBlob()
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.MessageUpdate,
                Text = "Hello",
                Id = "test-id",
                ChannelId = "channel-id",
                Conversation = new ConversationAccount() { Id = "convo-id" },
                Timestamp = new DateTimeOffset(),
                From = new ChannelAccount() { Id = "account-1" },
                Recipient = new ChannelAccount() { Id = "account-2" }
            };

            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Setup(x => x.DownloadTextAsync()).Returns(Task.FromResult(JsonConvert.SerializeObject(activity)));
            mockBlockBlob.Setup(x => x.OpenWriteAsync()).Returns(Task.FromResult(stream.Object));
            mockBlockBlob.Setup(x => x.SetMetadataAsync());

            var segment = new BlobResultSegment(new List<CloudBlockBlob>(), null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                It.IsAny<int>(),
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());
            mockContainer.Setup(x => x.GetBlockBlobReference(It.IsAny<string>())).Returns(mockBlockBlob.Object);

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.LogActivityAsync(activity);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetBlockBlobReference(It.IsAny<string>()), Times.Once);
            mockBlockBlob.Verify(x => x.OpenWriteAsync(), Times.Once);
            mockBlockBlob.Verify(x => x.SetMetadataAsync(), Times.Once);
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    It.IsAny<int>(),
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);
        }

        [Fact]
        public async Task LogActivityAsyncMessageDelete()
        {
            var activity = new Activity
            {
                Type = ActivityTypes.MessageDelete,
                Text = "Hello",
                Id = "test-id",
                ChannelId = "channel-id",
                Conversation = new ConversationAccount { Id = "convo-id" },
                Timestamp = new DateTimeOffset(),
                From = new ChannelAccount { Id = "account-1" },
                Recipient = new ChannelAccount { Id = "account-2" }
            };

            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Setup(x => x.DownloadTextAsync()).Returns(Task.FromResult(JsonConvert.SerializeObject(activity)));
            mockBlockBlob.Setup(x => x.OpenWriteAsync()).Returns(Task.FromResult(stream.Object));
            mockBlockBlob.Setup(x => x.SetMetadataAsync());

            var segment = new BlobResultSegment(new List<CloudBlockBlob> { mockBlockBlob.Object }, null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                It.IsAny<int>(),
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.LogActivityAsync(activity);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockBlockBlob.Verify(x => x.DownloadTextAsync(), Times.Exactly(2));
            mockBlockBlob.Verify(x => x.OpenWriteAsync(), Times.Once);
            mockBlockBlob.Verify(x => x.SetMetadataAsync(), Times.Exactly(2));
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    It.IsAny<int>(),
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptActivitiesAsyncValidations()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName);

            // No channel id. Should throw.
            await Assert.ThrowsAsync<ArgumentNullException>(() => blobTranscript.GetTranscriptActivitiesAsync(null, "convo-id"));

            // No conversation id. Should throw.
            await Assert.ThrowsAsync<ArgumentNullException>(() => blobTranscript.GetTranscriptActivitiesAsync("channel-id", null));
        }

        [Fact]
        public async Task GetTranscriptActivitiesAsync()
        {
            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));

            var segment = new BlobResultSegment(new List<CloudBlockBlob> { mockBlockBlob.Object }, null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                null,
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.GetTranscriptActivitiesAsync("channelId", "conversationId");

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    null,
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptActivitiesAsyncWithMetadata()
        {
            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Object.Metadata["Timestamp"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);

            var jsonString = JsonConvert.SerializeObject(new Activity());
            mockBlockBlob.Setup(x => x.DownloadTextAsync()).Returns(Task.FromResult(jsonString));

            var segment = new BlobResultSegment(new List<CloudBlockBlob> { mockBlockBlob.Object }, null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                null,
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.GetTranscriptActivitiesAsync("channelId", "conversationId");

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    null,
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptActivitiesAsyncContinuationToken()
        {
            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Object.Metadata["Timestamp"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            mockBlockBlob.SetupGet(x => x.Name).Returns("token-name");

            var jsonString = JsonConvert.SerializeObject(new Activity());
            mockBlockBlob.Setup(x => x.DownloadTextAsync()).Returns(Task.FromResult(jsonString));

            var segment = new BlobResultSegment(new List<CloudBlockBlob> { mockBlockBlob.Object }, null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                null,
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.GetTranscriptActivitiesAsync("channelId", "conversationId", "token-name");

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    null,
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task GetTranscriptActivitiesAsyncMultipleBlobs()
        {
            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Object.Metadata["Timestamp"] = DateTime.Now.ToString(CultureInfo.InvariantCulture);
            mockBlockBlob.SetupGet(x => x.Name).Returns("token-name");

            var jsonString = JsonConvert.SerializeObject(new Activity());
            mockBlockBlob.Setup(x => x.DownloadTextAsync()).Returns(Task.FromResult(jsonString));

            var segment = new BlobResultSegment(CreateSegment(21, mockBlockBlob.Object).ToList(), null);         

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                null,
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.GetTranscriptActivitiesAsync("channelId", "conversationId", "token-name");

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    null,
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ListTranscriptAsyncValidations()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName);

            // No channel id. Should throw.
            await Assert.ThrowsAsync<ArgumentNullException>(() => blobTranscript.ListTranscriptsAsync(null));
        }

        [Fact]
        public async Task ListTranscriptAsync()
        {
            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));

            var segment = new BlobResultSegment(new List<CloudBlockBlob> { mockBlockBlob.Object }, null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                null,
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.ListTranscriptsAsync("channelId", null);

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    null,
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);
        }

        [Fact]
        public async Task DeleteTranscriptAsyncValidations()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName);

            // No channel id. Should throw.
            await Assert.ThrowsAsync<ArgumentNullException>(() => blobTranscript.DeleteTranscriptAsync(null, "convo-id"));

            // No conversation id. Should throw.
            await Assert.ThrowsAsync<ArgumentNullException>(() => blobTranscript.DeleteTranscriptAsync("channel-id", null));
        }

        [Fact]
        public async Task DeleteTranscriptAsync()
        {
            var stream = new Mock<CloudBlobStream>();
            stream.SetupGet(x => x.CanWrite).Returns(true);

            var mockBlockBlob = new Mock<CloudBlockBlob>(new Uri("http://test/myaccount/blob"));
            mockBlockBlob.Setup(x => x.DeleteIfExistsAsync()).Returns(Task.FromResult<bool>(true));

            var segment = new BlobResultSegment(new List<CloudBlockBlob> { mockBlockBlob.Object }, null);

            var mockDirectory = new Mock<CloudBlobDirectory>();
            mockDirectory.Setup(x => x.ListBlobsSegmentedAsync(
                It.IsAny<bool>(),
                It.IsAny<BlobListingDetails>(),
                null,
                It.IsAny<BlobContinuationToken>(),
                It.IsAny<BlobRequestOptions>(),
                It.IsAny<OperationContext>())).Returns(Task.FromResult(segment));

            var mockContainer = new Mock<CloudBlobContainer>(new Uri("https://testuri.com"));
            mockContainer.Setup(x => x.GetDirectoryReference(It.IsAny<string>())).Returns(mockDirectory.Object);
            mockContainer.Setup(x => x.CreateIfNotExistsAsync());

            var mockBlobClient = new Mock<CloudBlobClient>(new Uri("https://testuri.com"), null);
            mockBlobClient.Setup(x => x.GetContainerReference(It.IsAny<string>())).Returns(mockContainer.Object);

            var storageAccount = CloudStorageAccount.Parse(ConnectionString);

            var blobTranscript = new AzureBlobTranscriptStore(storageAccount, ContainerName, mockBlobClient.Object);

            await blobTranscript.DeleteTranscriptAsync("channelId", "convo-id");

            mockBlobClient.Verify(x => x.GetContainerReference(It.IsAny<string>()), Times.Once);
            mockContainer.Verify(x => x.GetDirectoryReference(It.IsAny<string>()), Times.Once);
            mockDirectory.Verify(
                x => x.ListBlobsSegmentedAsync(
                    It.IsAny<bool>(),
                    It.IsAny<BlobListingDetails>(),
                    null,
                    It.IsAny<BlobContinuationToken>(),
                    It.IsAny<BlobRequestOptions>(),
                    It.IsAny<OperationContext>()), Times.Once);
            mockBlockBlob.Verify(x => x.DeleteIfExistsAsync(), Times.Once);
        }

        private static IEnumerable<CloudBlockBlob> CreateSegment(int count, CloudBlockBlob blob)
        {
            return Enumerable.Range(0, count).Select(x => blob);
        }
    }
}
