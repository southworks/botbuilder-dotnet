﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Actions
{
    /// <summary>
    /// Deletes a property from memory.
    /// </summary>
    public class DeleteProperties : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.DeleteProperties";

        [JsonConstructor]
        public DeleteProperties([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets properties to remove.
        /// </summary>
        /// <example>
        /// user.age will remove "age" from "user".
        /// </example>
        /// <value>
        /// Collection of property paths to remove.
        /// </value>
        [JsonProperty("properties")]
        public List<string> Properties { get; set; } = new List<string>();

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            // Ensure planning context
            if (dc is SequenceContext planning)
            {
                if (this.Properties?.Any() == true)
                {
                    foreach (var property in this.Properties)
                    {
                        dc.GetState().RemoveValue(property);
                    }
                }

                return await dc.EndDialogAsync();
            }
            else
            {
                throw new Exception("`DeleteProperty` should only be used in the context of an adaptive dialog.");
            }
        }
    }
}
