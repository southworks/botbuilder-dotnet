﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Tests
{
    /// <summary>
    /// Representas a transcript logger that writes activites to a <see cref="Trace"/> object.
    /// </summary>
    public class SimpleTextLogger : ITranscriptLogger
    {
        /// <summary>
        /// Log an activity to the transcript.
        /// </summary>
        /// <param name="activity">The activity to transcribe.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public Task LogActivityAsync(IActivity activity)
        {
            BotAssert.ActivityNotNull(activity);
            System.Diagnostics.Trace.TraceInformation($"{activity.Type}:{((Activity)activity).Text}");
            return Task.CompletedTask;
        }
    }

    [TestClass]
    public class DateTimePromptTests
    {
        [TestMethod]
        public async Task BasicDateTimePrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            TestAdapter adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            // Create and add number prompt to DialogSet.
            var dateTimePrompt = new DateTimePrompt("DateTimePrompt", defaultLocale: Culture.English);
            dialogs.Add(dateTimePrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync();
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = "What date would you like?" } };
                    await dc.PromptAsync("DateTimePrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var resolution = ((IList<DateTimeResolution>)results.Result).First();
                    var reply = MessageFactory.Text($"Timex:'{resolution.Timex}' Value:'{resolution.Value}'");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("What date would you like?")
            .Send("5th December 2018 at 9am")
            .AssertReply("Timex:'2018-12-05T09' Value:'2018-12-05 09:00:00'")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task MultipleResolutionsDateTimePrompt()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            TestAdapter adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            // Create and add number prompt to DialogSet.
            var dateTimePrompt = new DateTimePrompt("DateTimePrompt", defaultLocale: Culture.English);
            dialogs.Add(dateTimePrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = "What date would you like?" } };
                    await dc.PromptAsync("DateTimePrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var resolutions = (IList<DateTimeResolution>)results.Result;
                    var timexExpressions = resolutions.Select(r => r.Timex).Distinct();
                    var reply = MessageFactory.Text(string.Join(" ", timexExpressions));
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("What date would you like?")
            .Send("Wednesday 4 oclock")
            .AssertReply("XXXX-WXX-3T04 XXXX-WXX-3T16")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task DateTimePromptWithValidator()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            TestAdapter adapter = new TestAdapter()
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            // Create and add number prompt to DialogSet.
            var dateTimePrompt = new DateTimePrompt("DateTimePrompt", CustomValidator, defaultLocale: Culture.English);
            dialogs.Add(dateTimePrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new PromptOptions { Prompt = new Activity { Type = ActivityTypes.Message, Text = "What date would you like?" } };
                    await dc.PromptAsync("DateTimePrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var resolution = ((IList<DateTimeResolution>)results.Result).First();
                    var reply = MessageFactory.Text($"Timex:'{resolution.Timex}' Value:'{resolution.Value}'");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            })
            .Send("hello")
            .AssertReply("What date would you like?")
            .Send("5th December 2018 at 9am")
            .AssertReply("Timex:'2018-12-05' Value:'2018-12-05'")
            .StartTestAsync();
        }

        [TestMethod]
        public async Task DateTimePromptWithDataValidator()
        {
            var convoState = new ConversationState(new MemoryStorage());
            var dialogState = convoState.CreateProperty<DialogState>("dialogState");

            TestAdapter adapter = new TestAdapter()
                .Use(new TranscriptLoggerMiddleware(new SimpleTextLogger()))
                .Use(new AutoSaveStateMiddleware(convoState));

            // Create new DialogSet.
            var dialogs = new DialogSet(dialogState);

            // Create and add number prompt to DialogSet.
            var dateTimePrompt = new DateTimePrompt()
            {
                Id = "DateTimePrompt",
                DefaultLocale = Culture.English,
                MinValue = new DateTime(1920, 1,1,0,0,0),
                MaxValue = new DateTime(2050, 1, 1, 0, 0, 0),
                InitialPrompt = new Activity { Type = ActivityTypes.Message, Text = "What date would you like?" },
                RetryPrompt = new Activity { Type = ActivityTypes.Message, Text = "So...What date would you like?" },
                NoMatchResponse = new Activity { Type = ActivityTypes.Message, Text = "That isn't a date." },
                TooSmallResponse = new Activity { Type = ActivityTypes.Message, Text = "Too small." },
                TooLargeResponse = new Activity { Type = ActivityTypes.Message, Text = "Too large." },
            };
            dialogs.Add(dateTimePrompt);

            await new TestFlow(adapter, async (turnContext, cancellationToken) =>
            {
                var dc = await dialogs.CreateContextAsync(turnContext, cancellationToken);

                var results = await dc.ContinueDialogAsync(cancellationToken);
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var options = new DateTimePromptOptions()
                    {
                        //MinValue = DateTime.Now,
                        //MaxValue = DateTime.Now + TimeSpan.FromDays(14)
                    };
                    await dc.PromptAsync("DateTimePrompt", options, cancellationToken);
                }
                else if (results.Status == DialogTurnStatus.Complete)
                {
                    var resolution = ((IList<DateTimeResolution>)results.Result).First();
                    var reply = MessageFactory.Text($"Timex:'{resolution.Timex}' Value:'{resolution.Value}'");
                    await turnContext.SendActivityAsync(reply, cancellationToken);
                }
            })
            .Send("hello")
                .AssertReply("What date would you like?")
            .Send("xyz")
                .AssertReply("That isn't a date.")
                .AssertReply("So...What date would you like?")
            .Send("5th December 1918 at 9am")
                .AssertReply("Too small.")
                .AssertReply("So...What date would you like?")
            .Send("5th December 2051 at 9am")
                .AssertReply("Too large.")
                .AssertReply("So...What date would you like?")
            .Send("5th December 2018 at 9am")
                .AssertReply("Timex:'2018-12-05T09' Value:'2018-12-05 09:00:00'")
            .StartTestAsync();
        }

        private Task<bool> CustomValidator(PromptValidatorContext<IList<DateTimeResolution>> prompt, CancellationToken cancellationToken)
        {
            if (prompt.Recognized.Succeeded)
            {
                var resolution = prompt.Recognized.Value.First();
                // re-write the resolution to just include the date part.
                var rewrittenResolution = new DateTimeResolution
                {
                    Timex = resolution.Timex.Split('T')[0],
                    Value = resolution.Value.Split(' ')[0]
                };
                prompt.Recognized.Value = new List<DateTimeResolution> { rewrittenResolution };
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
    }
}

