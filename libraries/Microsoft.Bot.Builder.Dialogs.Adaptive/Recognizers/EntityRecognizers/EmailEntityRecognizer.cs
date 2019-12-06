﻿using System.Collections.Generic;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Sequence;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers
{
    public class EmailEntityRecognizer : EntityRecognizer
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.EmailEntityRecognizer";

        public EmailEntityRecognizer()
        {
        }

        protected override List<ModelResult> Recognize(string text, string culture)
        {
            return SequenceRecognizer.RecognizeEmail(text, culture);
        }
    }
}
