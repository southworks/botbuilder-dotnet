﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Data;

namespace Microsoft.Bot.Builder.Dialogs.Memory.Scopes
{
    /// <summary>
    /// UserMemoryScope represents User scoped memory.
    /// </summary>
    /// <remarks>This relies on the UserState object being accessible from turnContext.TurnState.</remarks>
    public class UserMemoryScope : BotStateMemoryScope<UserState>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserMemoryScope"/> class.
        /// </summary>
        /// <param name="userState">UserState to bind the UserMemory to.</param>
        /// <param name="propertyName">Optional alternate propertyName to store UserMemoryScope in.</param>
        public UserMemoryScope(UserState userState, string propertyName = null)
            : base(ScopePath.USER, userState, propertyName)
        {
        }
    }
}
