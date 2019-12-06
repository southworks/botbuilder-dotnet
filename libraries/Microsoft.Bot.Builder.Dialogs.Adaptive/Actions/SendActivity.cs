﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Actions
{
    /// <summary>
    /// Send an activity back to the user.
    /// </summary>
    public class SendActivity : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.SendActivity";

        public SendActivity(Activity activity, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
            this.Activity = new StaticActivityTemplate(activity);
        }

        [JsonConstructor]
        public SendActivity(string text = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
            this.Activity = new ActivityTemplate(text ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets template for the activity.
        /// </summary>
        /// <value>
        /// Template for the activity.
        /// </value>
        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            var activity = await Activity.BindToData(dc.Context, dc.GetState()).ConfigureAwait(false);
            var response = await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            return await dc.EndDialogAsync(response, cancellationToken).ConfigureAwait(false);
        }

        protected override string OnComputeId()
        {
            if (Activity is ActivityTemplate at)
            {
                return $"{this.GetType().Name}({Ellipsis(at.Template.Trim(), 30)})";
            }

            return $"{this.GetType().Name}('{Ellipsis(Activity?.ToString().Trim(), 30)}')";
        }

        private static string Ellipsis(string text, int length)
        {
            if (text.Length <= length)
            {
                return text;
            }

            int pos = text.IndexOf(" ", length);

            if (pos >= 0)
            {
                return text.Substring(0, pos) + "...";
            }

            return text;
        }
    }
}
