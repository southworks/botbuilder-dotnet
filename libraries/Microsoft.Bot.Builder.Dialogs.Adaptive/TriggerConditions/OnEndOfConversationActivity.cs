﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions
{
    /// <summary>
    /// Actions triggered when EndOfConversationActivity is received.
    /// </summary>
    public class OnEndOfConversationActivity : OnActivity
    {
        [JsonProperty("$kind")]
        public new const string DeclarativeType = "Microsoft.OnEndOfConversationActivity";

        [JsonConstructor]
        public OnEndOfConversationActivity(List<Dialog> actions = null, string constraint = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(type: ActivityTypes.EndOfConversation, actions: actions, constraint: constraint, callerPath: callerPath, callerLine: callerLine)
        {
        }
    }
}
