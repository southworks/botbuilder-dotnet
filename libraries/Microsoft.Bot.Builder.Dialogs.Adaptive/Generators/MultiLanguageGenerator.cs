﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Generators
{
    /// <summary>
    /// ILanguageGenerator which uses implements a map of locale->ILanguageGenerator for the locale and has a policy which controls fallback (try en-us -> en -> default).
    /// </summary>
    public class MultiLanguageGenerator : MultiLanguageGeneratorBase
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.MultiLanguageGenerator";

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiLanguageGenerator"/> class.
        /// </summary>
        public MultiLanguageGenerator()
        {
        }

        /// <summary>
        /// Gets or sets the language generators for multiple languages.
        /// </summary>
        /// <value>
        /// The language generators for multiple languages.
        /// </value>
        [JsonProperty("languageGenerators")]
        public ConcurrentDictionary<string, ILanguageGenerator> LanguageGenerators { get; set; } = new ConcurrentDictionary<string, ILanguageGenerator>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Implementation of lookup by locale.  This uses internal dictionary to lookup.
        /// </summary>
        /// <param name="context">Context for the current turn of conversation with the user.</param>\
        /// <param name="locale">locale.</param>
        /// <param name="languageGenerator">generator to return.</param>
        /// <returns>true if found.</returns>
        public override bool TryGetGenerator(ITurnContext context, string locale, out ILanguageGenerator languageGenerator)
        {
            return this.LanguageGenerators.TryGetValue(locale, out languageGenerator);
        }
    }
}
