﻿using System.Collections.Generic;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Number;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers
{
    public class PercentageEntityRecognizer : EntityRecognizer
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.PercentageEntityRecognizer";

        public PercentageEntityRecognizer()
        {
        }

        protected override List<ModelResult> Recognize(string text, string culture)
        {
            return NumberRecognizer.RecognizePercentage(text, culture);
        }
    }
}
