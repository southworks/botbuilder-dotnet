﻿using System.Collections.Generic;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.Sequence;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Recognizers
{
    public class PhoneNumberEntityRecognizer : EntityRecognizer
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.PhoneNumberEntityRecognizer";

        public PhoneNumberEntityRecognizer()
        {
        }

        protected override List<ModelResult> Recognize(string text, string culture)
        {
            return SequenceRecognizer.RecognizePhoneNumber(text, culture);
        }
    }
}
