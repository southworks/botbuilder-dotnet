// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Connector
{
    /// <summary>
    /// A class representing the options for <see cref="Microsoft.Bot.Connector.ConnectorClient"/>.
    /// </summary>
    public class ConnectorClientOptions
    {
        /// <summary>
        /// Gets or sets the user agent when sending the request.
        /// </summary>
        /// <value>
        /// The user agent.
        /// </value>
        public string UserAgent { get; set; }
    }
}
