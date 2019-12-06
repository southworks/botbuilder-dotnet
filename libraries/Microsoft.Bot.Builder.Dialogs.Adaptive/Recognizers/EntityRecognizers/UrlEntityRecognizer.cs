﻿using System.Collections.Generic;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Sequence;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers
{
    public class UrlEntityRecognizer : EntityRecognizer
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.UrlEntityRecognizer";

        public UrlEntityRecognizer()
        {
        }

        protected override List<ModelResult> Recognize(string text, string culture)
        {
            return SequenceRecognizer.RecognizeURL(text, culture);
        }
    }
}
