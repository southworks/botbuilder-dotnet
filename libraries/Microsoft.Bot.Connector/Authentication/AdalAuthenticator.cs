﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Bot.Connector.Authentication
{
    public class AdalAuthenticator
    {
        private const string MsalTemporarilyUnavailable = "temporarily_unavailable";

        // Semaphore to control concurrency while refreshing tokens from ADAL.
        // Whenever a token expires, we want only one request to retrieve a token.
        // Cached requests take less than 0.1 millisecond to resolve, so the semaphore doesn't hurt performance under load tests
        // unless we have more than 10,000 requests per second, but in that case other things would break first.
        private static Semaphore tokenRefreshSemaphore = new Semaphore(1, 1);

        private static readonly TimeSpan SemaphoreTimeout = TimeSpan.FromSeconds(10);

        // Depending on the responses we get from the service, we update a shared retry policy with the RetryAfter header
        // from the HTTP 429 we receive.
        // When everything seems to be OK, this retry policy will be empty.
        // The reason for this is that if a request gets throttled, even if we wait to retry that, another thread will try again right away.
        // With the shared retry policy, if a request gets throttled, we know that other threads have to wait as well.
        // This variable is guarded by the authContextSemaphore semphore. Don't modify it outside of the semaphore scope.
        private static volatile RetryParams currentRetryPolicy;

        // Our ADAL context. Acquires tokens and manages token caching for us.
        private readonly AuthenticationContext authContext;

        private readonly ClientCredential clientCredential;

        private readonly OAuthConfiguration openAuthConfig;

        public AdalAuthenticator(ClientCredential clientCredential, OAuthConfiguration openAuthConfig, HttpClient customHttpClient = null)
        {
            this.openAuthConfig = openAuthConfig ?? throw new ArgumentNullException(nameof(openAuthConfig));
            this.clientCredential = clientCredential ?? throw new ArgumentNullException(nameof(clientCredential));

            if (customHttpClient != null)
            {
                this.authContext = new AuthenticationContext(openAuthConfig.Authority, true, new TokenCache(), customHttpClient);
            }
            else
            {
                this.authContext = new AuthenticationContext(openAuthConfig.Authority);
            }
        }

        public async Task<AuthenticationResult> GetTokenAsync(bool forceRefresh = false)
        {
            return await Retry.Run(
                task: () => AcquireTokenAsync(forceRefresh),
                retryExceptionHandler: (ex, ct) => HandleAdalException(ex, ct)).ConfigureAwait(false);
        }

        private async Task<AuthenticationResult> AcquireTokenAsync(bool forceRefresh = false)
        {
            bool acquired = false;

            if (forceRefresh)
            {
                authContext.TokenCache.Clear();
            }

            try
            {
                // The ADAL client team recommends limiting concurrency of calls. When the Token is in cache there is never
                // contention on this semaphore, but when tokens expire there is some. However, after measuring performance
                // with and without the semaphore (and different configs for the semaphore), not limiting concurrency actually
                // results in higher response times overall. Without the use of this semaphore calls to AcquireTokenAsync can take up
                // to 5 seconds under high concurrency scenarios.
                acquired = tokenRefreshSemaphore.WaitOne(SemaphoreTimeout);

                // If we are allowed to enter the semaphore, acquire the token.
                if (acquired)
                {
                    // Acquire token async using MSAL.NET
                    // https://github.com/AzureAD/azure-activedirectory-library-for-dotnet
                    // Given that this is a ClientCredential scenario, it will use the cache without the
                    // need to call AcquireTokenSilentAsync (which is only for user credentials).
                    // Scenario details: https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Client-credential-flows#it-uses-the-application-token-cache
                    var res = await authContext.AcquireTokenAsync(openAuthConfig.Scope, this.clientCredential).ConfigureAwait(false);

                    // This means we acquired a valid token successfully. We can make our retry policy null.
                    // Note that the retry policy is set under the semaphore so no additional synchronization is needed.
                    if (currentRetryPolicy != null)
                    {
                        currentRetryPolicy = null;
                    }

                    return res;
                }
                else
                {
                    // If the token is taken, it means that one thread is trying to acquire a token from the server.
                    // If we already received information about how much to throttle, it will be in the currentRetryPolicy.
                    // Use that to inform our next delay before trying.
                    throw new ThrottleException() { RetryParams = currentRetryPolicy };
                }
            }
            catch (Exception ex)
            {
                // If we are getting throttled, we set the retry policy according to the RetryAfter headers
                // that we receive from the auth server.
                // Note that the retry policy is set under the semaphore so no additional synchronization is needed.
                if (IsAdalServiceUnavailable(ex))
                {
                    currentRetryPolicy = ComputeAdalRetry(ex);
                }

                throw;
            }
            finally
            {
                // Always release the semaphore if we acquired it.
                if (acquired)
                {
                    tokenRefreshSemaphore.Release();
                }
            }
        }

        private RetryParams HandleAdalException(Exception ex, int currentRetryCount)
        {
            if (IsAdalServiceUnavailable(ex))
            {
                return ComputeAdalRetry(ex);
            }
            else if (ex is ThrottleException)
            {
                // This is an exception that we threw, with knowledge that
                // one of our threads is trying to acquire a token from the server
                // Use the retry parameters recommended in the exception
                ThrottleException throttlException = (ThrottleException)ex;
                return throttlException.RetryParams ?? RetryParams.DefaultBackOff(currentRetryCount);
            }
            else
            {
                // We end up here is the exception is not an ADAL exception. An example, is under high traffic
                // where we could have a timeout waiting to acquire a token, waiting on the semaphore.
                // If we hit a timeout, we want to retry a reasonable number of times.
                return RetryParams.DefaultBackOff(currentRetryCount);
            }
        }

        private bool IsAdalServiceUnavailable(Exception ex)
        {
            AdalServiceException adalServiceException = ex as AdalServiceException;
            if (adalServiceException == null)
            {
                return false;
            }

            // When the Service Token Server (STS) is too busy because of “too many requests”,
            // it returns an HTTP error 429
            return adalServiceException.ErrorCode == MsalTemporarilyUnavailable || adalServiceException.StatusCode == 429;
        }

        private RetryParams ComputeAdalRetry(Exception ex)
        {
            if (ex is AdalServiceException)
            {
                AdalServiceException adalServiceException = (AdalServiceException)ex;

                // When the Service Token Server (STS) is too busy because of “too many requests”,
                // it returns an HTTP error 429 with a hint about when you can try again (Retry-After response field) as a delay in seconds
                if (adalServiceException.ErrorCode == MsalTemporarilyUnavailable || adalServiceException.StatusCode == 429)
                {
                    RetryConditionHeaderValue retryAfter = adalServiceException.Headers.RetryAfter;

                    // Depending on the service, the recommended retry time may be in retryAfter.Delta or retryAfter.Date. Check both.
                    if (retryAfter != null && retryAfter.Delta.HasValue)
                    {
                        return new RetryParams(retryAfter.Delta.Value);
                    }
                    else if (retryAfter != null && retryAfter.Date.HasValue)
                    {
                        return new RetryParams(retryAfter.Date.Value.Offset);
                    }

                    // We got a 429 but didn't get a specific back-off time. Use the default
                    return RetryParams.DefaultBackOff(0);
                }
            }

            return RetryParams.DefaultBackOff(0);
        }
    }
}
