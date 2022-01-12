// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Tests;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Bot.Builder.Azure.Tests
{
    [Trait("TestCategory", "Storage")]
    [Trait("TestCategory", "Storage - CosmosDB Partitioned")]
    [Collection("CosmosDb")]
    public class CosmosDbPartitionStorageTests : StorageBaseTests, IAsyncLifetime, IClassFixture<CosmosDbPartitionStorageFixture>
    {
        private IStorage _storage;

        public CosmosDbPartitionStorageTests(CosmosDbFixture cosmosDbFixture)
        {
            _storage = new CosmosDbPartitionedStorage(
                new CosmosDbPartitionedStorageOptions
                {
                    AuthKey = CosmosDbFixture.CosmosAuthKey,
                    ContainerId = CosmosDbPartitionStorageFixture.CosmosCollectionName,
                    CosmosDbEndpoint = CosmosDbFixture.CosmosServiceEndpoint,
                    DatabaseId = CosmosDbPartitionStorageFixture.CosmosDatabaseName,
                });
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _storage = null;
        }

        [Fact]
        public void Constructor_Should_Throw_On_InvalidOptions()
        {
            // No Options. Should throw.
            Assert.Throws<ArgumentNullException>(() => new CosmosDbPartitionedStorage(null));

            // No JsonSerializer. Should throw.
            Assert.Throws<ArgumentException>(() => new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions(), null));

            // No Endpoint. Should throw.
            Assert.Throws<ArgumentException>(() => new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions()
            {
                AuthKey = "test",
                ContainerId = "testId",
                DatabaseId = "testDb",
                CosmosDbEndpoint = null,
            }));

            // No Auth Key. Should throw.
            Assert.Throws<ArgumentException>(() => new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions()
            {
                AuthKey = null,
                ContainerId = "testId",
                DatabaseId = "testDb",
                CosmosDbEndpoint = "testEndpoint",
            }));

            // No Database Id. Should throw.
            Assert.Throws<ArgumentException>(() => new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions()
            {
                AuthKey = "testAuthKey",
                ContainerId = "testId",
                DatabaseId = null,
                CosmosDbEndpoint = "testEndpoint",
            }));

            // No Container Id. Should throw.
            Assert.Throws<ArgumentException>(() => new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions()
            {
                AuthKey = "testAuthKey",
                ContainerId = null,
                DatabaseId = "testDb",
                CosmosDbEndpoint = "testEndpoint",
            }));

            // Invalid Row Key characters in KeySuffix
            Assert.Throws<ArgumentException>(() => new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions()
            {
                AuthKey = "testAuthKey",
                ContainerId = "testId",
                DatabaseId = "testDb",
                CosmosDbEndpoint = "testEndpoint",
                KeySuffix = "?#*test",
                CompatibilityMode = false
            }));

            Assert.Throws<ArgumentException>(() => new CosmosDbPartitionedStorage(new CosmosDbPartitionedStorageOptions()
            {
                AuthKey = "testAuthKey",
                ContainerId = "testId",
                DatabaseId = "testDb",
                CosmosDbEndpoint = "testEndpoint",
                KeySuffix = "thisisatest",
                CompatibilityMode = true
            }));
        }

        [SkippableFact]
        public async Task ReadingEmptyKeysReturnsEmptyDictionary()
        {
            var state = await _storage.ReadAsync(new string[] { });
            Assert.IsType<Dictionary<string, object>>(state);
            Assert.Equal(0, state.Count);
        }

        [SkippableFact]
        public async Task ReadingNullKeysThrowException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storage.ReadAsync(null));
        }

        [SkippableFact]
        public async Task WritingNullStoreItemsThrowException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await _storage.WriteAsync(null));
        }

        [SkippableFact]
        public async Task WritingNoStoreItemsDoesntThrow()
        {
            var changes = new Dictionary<string, object>();
            await _storage.WriteAsync(changes);
        }

        [SkippableFact]
        public async Task DeletingNullStoreItemsThrowException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _storage.DeleteAsync(null));
        }
    }
}
