﻿using System.Collections.Generic;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers
{
    public class TemperatureEntityRecognizer : EntityRecognizer
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.TemperatureEntityRecognizer";

        public TemperatureEntityRecognizer()
        {
        }

        protected override List<ModelResult> Recognize(string text, string culture)
        {
            return NumberWithUnitRecognizer.RecognizeTemperature(text, culture);
        }
    }
}
