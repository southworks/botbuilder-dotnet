﻿// Copyright (c) Microsoft Corporation.All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Adapters.Twilio
{
    public interface ITwilioApi
    {
        void LogIn(string username, string password);

        Task<object> CreateMessageResourceAsync(object messageOptions);
    }
}
