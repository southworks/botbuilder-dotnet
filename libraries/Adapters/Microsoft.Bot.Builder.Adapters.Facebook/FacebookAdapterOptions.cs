﻿// Copyright(c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Adapters.Facebook
{
    public class FacebookAdapterOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookAdapterOptions"/> class.
        /// </summary>
        /// <param name="apiHost">A token for validating the origin of incoming webhooks.</param>
        /// <param name="apiVersion">A token for a bot to work on a single workspace.</param>
        /// <param name="verifyToken">The token used to validate that incoming webhooks are originated from Slack.</param>
        /// <param name="appSecret">The app secret.</param>
        /// <param name="accessToken">The access token.</param>
        public FacebookAdapterOptions(string verifyToken, string appSecret, string accessToken, string apiHost = "graph.facebook.com", string apiVersion = "v3.2")
        {
            VerifyToken = verifyToken;
            AppSecret = appSecret;
            AccessToken = accessToken;
            ApiHost = apiHost;
            ApiVersion = apiVersion;
        }

        /// <summary>
        /// Gets or sets the alternate root url used to contruct calls to Facebook's API.  Defaults to 'graph.facebook.com' but can be changed (for mocking, proxy, etc).
        /// </summary>
        /// <value>The API host.</value>
        public string ApiHost { get; set; }

        /// <summary>
        /// Gets or sets the alternate API version used to construct calls to Facebook's API. Defaults to v3.2.
        /// </summary>
        /// <value>The API version.</value>
        public string ApiVersion { get; set; }

        /// <summary>
        /// Gets or sets the "verify token" used to initially create and verify the Webhooks subscription settings on Facebook's developer portal.
        /// </summary>
        /// <value>The verification token.</value>
        public string VerifyToken { get; set; }

        /// <summary>
        /// Gets or sets the "app secret" from the "basic settings" page from your app's configuration in the Facebook developer portal.
        /// </summary>
        /// <value>The app secret.</value>
        public string AppSecret { get; set; }

        /// <summary>
        /// Gets or sets the access token.
        /// When bound to a single page, use `access_token` to specify the "page access token" provided in the Facebook developer portal's "Access Tokens" widget of the "Messenger Settings" page.
        /// </summary>
        /// <value>The access token.</value>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the function described below.
        /// When bound to multiple teams, provide a function that, given a page id, will return the page access token for that page.
        /// </summary>
        /// <param name="pageId">The page Id.</param>
        /// <returns>The access token for the page.</returns>
        public virtual Task<string> GetAccessTokenForPageAsync(string pageId)
        {
            throw new NotImplementedException();
        }
    }
}
