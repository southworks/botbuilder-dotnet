﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Dialogs.Memory;
using Microsoft.Recognizers.Text.DateTime.Chinese;

namespace Microsoft.Bot.Builder.Dialogs
{
    public static class DialogContextExtensions
    {
        /// <summary>
        /// Get access to the new DialogStateManager for the given context.
        /// </summary>
        /// <param name="dc">dialog context to get the DialogStateManager for.</param>
        /// <returns>DialogStateManager.</returns>
        public static DialogStateManager GetState(this DialogContext dc)
        {
            return new DialogStateManager(dc);
        }

        public static void SetStateConfiguration(this DialogContext dc, DialogStateManagerConfiguration configuration)
        {
            dc.Context.TurnState.Set(configuration ?? throw new ArgumentNullException(nameof(configuration)));
        }
    }
}
