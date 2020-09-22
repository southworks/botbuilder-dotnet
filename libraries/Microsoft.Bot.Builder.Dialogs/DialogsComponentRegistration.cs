﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs.Memory;
using Microsoft.Bot.Builder.Dialogs.Memory.PathResolvers;
using Microsoft.Bot.Builder.Dialogs.Memory.Scopes;

namespace Microsoft.Bot.Builder.Dialogs
{
    /// <summary>
    /// Makes Dialogs components available to the system registering functionality.
    /// </summary>
    public class DialogsComponentRegistration : ComponentRegistration, IComponentMemoryScopes, IComponentPathResolvers
    {
        /// <summary>
        /// Gets the Dialogs Memory Scopes.
        /// </summary>
        /// <returns>A collection of <see cref="MemoryScope"/>.</returns>
        public virtual IEnumerable<MemoryScope> GetMemoryScopes()
        {
            yield return new TurnMemoryScope();
            yield return new SettingsMemoryScope();
            yield return new DialogMemoryScope();
            yield return new DialogContextMemoryScope();
            yield return new DialogClassMemoryScope();
            yield return new ClassMemoryScope();
            yield return new ThisMemoryScope();
            yield return new ConversationMemoryScope();
            yield return new UserMemoryScope();
        }

        /// <summary>
        /// Gets the Dialogs Path Resolvers.
        /// </summary>
        /// <returns>A collection of <see cref="IPathResolver"/>.</returns>
        public virtual IEnumerable<IPathResolver> GetPathResolvers()
        {
            yield return new DollarPathResolver();
            yield return new HashPathResolver();
            yield return new AtAtPathResolver();
            yield return new AtPathResolver();
            yield return new PercentPathResolver();
        }
    }
}
