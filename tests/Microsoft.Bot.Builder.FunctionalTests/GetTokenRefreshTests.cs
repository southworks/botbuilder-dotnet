﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.FunctionalTests
{
    [TestClass]
    #if !FUNCTIONALTESTS
    [Ignore("These integration tests run only when FUNCTIONALTESTS is defined")]
    #endif
    public class GetTokenRefreshTests
    {
        private string testAppId = null;
        private string testPassword = null;

        public GetTokenRefreshTests()
        {
        }

        [TestMethod]
        public async Task TokenTests_GetCredentialsWorks()
        {
            GetEnvironmentVarsTestAppIdPassword();
            MicrosoftAppCredentials credentials = new MicrosoftAppCredentials(testAppId, testPassword);
            var result = await credentials.GetTokenAsync();
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public async Task TokenTests_RefreshTokenWorks()
        {
            GetEnvironmentVarsTestAppIdPassword();
            MicrosoftAppCredentials credentials = new MicrosoftAppCredentials(testAppId, testPassword);
            var result = await credentials.GetTokenAsync();
            Assert.IsNotNull(result);
            var result2 = await credentials.GetTokenAsync();
            Assert.Equals(result, result2);
            var result3 = await credentials.GetTokenAsync(true);
            Assert.IsNotNull(result3);
            Assert.IsNotNull(result2, result3);
        }

        [TestMethod]
        public async Task TokenTests_RefreshTestLoad()
        {
            GetEnvironmentVarsTestAppIdPassword();
            MicrosoftAppCredentials credentials = new MicrosoftAppCredentials(testAppId, testPassword);
            List<Task<string>> tasks = new List<Task<string>>();
            for (int i = 0; i < 1000; i++)
            {
                tasks.Add(credentials.GetTokenAsync());
            }

            string prevResult = null;
            foreach (var item in tasks)
            {
                string result = await item;
                Assert.IsNotNull(result);
                if (prevResult != null)
                {
                    Assert.Equals(prevResult, result);
                }

                prevResult = result;
            }

            tasks.Clear();
            for (int i = 0; i < 1000; i++)
            {
                if (i % 100 == 50)
                {
                    tasks.Add(credentials.GetTokenAsync(true));
                }
                else
                {
                    tasks.Add(credentials.GetTokenAsync());
                }
            }

            HashSet<string> results = new HashSet<string>();
            for (int i = 0; i < 1000; i++)
            {
                string result = await tasks[i];
                if (i == 0)
                {
                    results.Add(result);
                    Assert.IsNotNull(result);
                }

                if (prevResult != null)
                {
                    if (i % 100 == 50)
                    {
                        Assert.IsTrue(!results.Contains(result));
                        results.Add(result);
                    }
                    else
                    {
                        // Xunit.Assert.Contains(result, results);
                        Assert.IsTrue(results.Contains(result));
                    }
                }
            }
        }

        private void GetEnvironmentVarsTestAppIdPassword()
        {
            if (string.IsNullOrWhiteSpace(testAppId) || string.IsNullOrWhiteSpace(testPassword))
            {
                testAppId = Environment.GetEnvironmentVariable("TestAppId");
                if (string.IsNullOrWhiteSpace(testAppId))
                {
                    throw new Exception("Environment variable 'TestAppId' not found.");
                }

                testPassword = Environment.GetEnvironmentVariable("TestPassword");

                if (string.IsNullOrWhiteSpace(testPassword))
                {
                    throw new Exception("Environment variable 'TestPassword' not found.");
                }
            }
        }
    }
}
