﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions
{
    /// <summary>
    /// Actions triggered when an MessageActivity is received.
    /// </summary>
    /// <remarks>
    /// The default behavior for an Adaptive Dialog is to process this event and run the configured Recognizer against the input, triggering OnIntent/OnUnknownIntent events.
    /// Defining this trigger condition overrides that behavior with custom steps.
    /// </remarks>
    public class OnMessageActivity : OnActivity
    {
        [JsonProperty("$kind")]
        public new const string DeclarativeType = "Microsoft.OnMessageActivity";

        [JsonConstructor]
        public OnMessageActivity(List<Dialog> actions = null, string constraint = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(type: ActivityTypes.Message, actions: actions, constraint: constraint, callerPath: callerPath, callerLine: callerLine)
        {
        }
    }
}
