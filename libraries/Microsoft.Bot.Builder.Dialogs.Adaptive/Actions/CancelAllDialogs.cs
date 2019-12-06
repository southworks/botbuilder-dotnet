﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Expressions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Actions
{
    /// <summary>
    /// Command to cancel all of the current dialogs by emitting an event which must be caught to prevent cancelation from propagating.
    /// </summary>
    public class CancelAllDialogs : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.CancelAllDialogs";

        [JsonConstructor]
        public CancelAllDialogs([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets event name. 
        /// </summary>
        /// <value>
        /// Event name. 
        /// </value>
        [JsonProperty("eventName")]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets value expression for EventValue.
        /// </summary>
        /// <value>
        /// Value expression for EventValue.
        /// </value>
        [JsonProperty("eventValue")]
        public string EventValue { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            object eventValue = null;
            if (this.EventValue != null)
            {
                eventValue = new ExpressionEngine().Parse(this.EventValue).TryEvaluate(dc.GetState());
            }

            if (dc.Parent == null)
            {
                return await dc.CancelAllDialogsAsync(true, EventName, eventValue, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var turnResult = await dc.Parent.CancelAllDialogsAsync(true, EventName, eventValue, cancellationToken).ConfigureAwait(false);
                turnResult.ParentEnded = true;
                return turnResult;
            }
        }
    }
}
