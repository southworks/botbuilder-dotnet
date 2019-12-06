﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Expressions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Input
{
    public abstract class InputDialog : Dialog
    {
#pragma warning disable SA1310 // Field should not contain underscore.
        protected const string TURN_COUNT_PROPERTY = "this.turnCount";
        protected const string VALUE_PROPERTY = "this.value";
#pragma warning restore SA1310 // Field should not contain underscore.

        private Expression allowInterruptions;

        /// <summary>
        /// Gets or sets a value indicating whether the input should always prompt the user regardless of there being a value or not.
        /// </summary>
        /// <value>
        /// A value indicating whether the input should always prompt the user regardless of there being a value or not.
        /// </value>
        [JsonProperty("alwaysPrompt")]
        public bool AlwaysPrompt { get; set; } = false;

        /// <summary>
        /// Gets or sets intteruption policy. 
        /// </summary>
        /// <example>
        /// "true".
        /// </example>
        /// <value>
        /// Intteruption policy. 
        /// </value>
        [JsonProperty("allowInterruptions")]
        public string AllowInterruptions
        {
            get { return allowInterruptions?.ToString(); }
            set { allowInterruptions = value != null ? new ExpressionEngine().Parse(value) : null; }
        }

        /// <summary>
        /// Gets or sets the value expression which the input will be bound to.
        /// </summary>
        /// <value>
        /// The value expression which the input will be bound to.
        /// </value>
        [JsonProperty("property")]
        public string Property { get; set; }

        /// <summary>
        /// Gets or sets a value expression which can be used to intialize the input prompt.
        /// </summary>
        /// <remarks>
        /// An example of how to use this would be to use an entity expression such as @age to fill the value for this dialog
        /// that is configured to go into $age dialog property.
        /// </remarks>
        /// <value>
        /// A value expression which can be used to intialize the input prompt.
        /// </value>
        [JsonProperty("value")]
        public string Value { get; set; }

        /// <summary>
        /// Gets or sets the activity to send to the user.
        /// </summary>
        /// <value>
        /// The activity to send to the user.
        /// </value>
        [JsonProperty("prompt")]
        public ITemplate<Activity> Prompt { get; set; }

        /// <summary>
        /// Gets or sets the activity template for retrying prompt.
        /// </summary>
        /// <value>
        /// The activity template for retrying prompt.
        /// </value>
        [JsonProperty("unrecognizedPrompt")]
        public ITemplate<Activity> UnrecognizedPrompt { get; set; }

        /// <summary>
        /// Gets or sets the activity template to send to the user whenever the value provided is invalid.
        /// </summary>
        /// <value>
        /// The activity template to send to the user whenever the value provided is invalid.
        /// </value>
        [JsonProperty("invalidPrompt")]
        public ITemplate<Activity> InvalidPrompt { get; set; }

        /// <summary>
        /// Gets or sets the activity template to send when MaxTurnCount has been reached and the default value is used.
        /// </summary>
        /// <value>
        /// The activity template to send when MaxTurnCount has been reached and the default value is used.
        /// </value>
        [JsonProperty("defaultValueResponse")]
        public ITemplate<Activity> DefaultValueResponse { get; set; }

        /// <summary>
        /// Gets or sets the expressions to run to validate the input.
        /// </summary>
        /// <value>
        /// The expressions to run to validate the input.
        /// </value>
        [JsonProperty("validations")]
        public List<string> Validations { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets maximum number of times to ask the user for this value before the dilog gives up.
        /// </summary>
        /// <value>
        /// Maximum number of times to ask the user for this value before the dilog gives up.
        /// </value>
        [JsonProperty("maxTurnCount")]
        public int? MaxTurnCount { get; set; }

        /// <summary>
        /// Gets or sets the default value for the input dialog when MaxTurnCount is exceeded.
        /// </summary>
        /// <value>
        /// The default value for the input dialog when MaxTurnCount is exceeded.
        /// </value>
        [JsonProperty("defaultValue")]
        public string DefaultValue { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (dc == null)
            {
                throw new ArgumentNullException(nameof(dc));
            }

            var op = OnInitializeOptions(dc, options);
            dc.GetState().SetValue(ThisPath.OPTIONS, op);
            dc.GetState().SetValue(TURN_COUNT_PROPERTY, 0);

            // If AlwaysPrompt is set to true, then clear Property value for turn 0.
            if (!string.IsNullOrEmpty(this.Property) && this.AlwaysPrompt)
            {
                dc.GetState().SetValue(this.Property, null);
            }

            var state = this.AlwaysPrompt ? InputState.Missing : await this.RecognizeInput(dc, 0);
            if (state == InputState.Valid)
            {
                var input = dc.GetState().GetValue<object>(VALUE_PROPERTY);

                // set property
                dc.GetState().SetValue(this.Property, input);

                // return as result too
                return await dc.EndDialogAsync(input);
            }
            else
            {
                // turnCount should increase here, because you want when nextTurn comes in
                // We will set the turn count to 1 so the input will not pick from "dialog.value"
                // and instead go with "turn.activity.text"
                dc.GetState().SetValue(TURN_COUNT_PROPERTY, 1);
                return await this.PromptUser(dc, state);
            }
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var activity = dc.Context.Activity;
            if (activity.Type != ActivityTypes.Message)
            {
                return Dialog.EndOfTurn;
            }

            var interrupted = dc.GetState().GetValue<bool>(TurnPath.INTERRUPTED, () => false);
            var turnCount = dc.GetState().GetValue<int>(TURN_COUNT_PROPERTY, () => 0);

            // Perform base recognition
            var state = await this.RecognizeInput(dc, interrupted ? 0 : turnCount);

            if (state == InputState.Valid)
            {
                var input = dc.GetState().GetValue<object>(VALUE_PROPERTY);

                // set output property
                if (!string.IsNullOrEmpty(this.Property))
                {
                    dc.GetState().SetValue(this.Property, input);
                }

                return await dc.EndDialogAsync(input).ConfigureAwait(false);
            }
            else if (this.MaxTurnCount == null || turnCount < this.MaxTurnCount)
            {
                // increase the turnCount as last step
                dc.GetState().SetValue(TURN_COUNT_PROPERTY, turnCount + 1);
                return await this.PromptUser(dc, state).ConfigureAwait(false);
            }
            else
            {
                if (this.DefaultValue != null)
                {
                    var (value, error) = new ExpressionEngine().Parse(this.DefaultValue).TryEvaluate(dc.GetState());
                    if (this.DefaultValueResponse != null)
                    {
                        var response = await this.DefaultValueResponse.BindToData(dc.Context, dc.GetState()).ConfigureAwait(false);
                        await dc.Context.SendActivityAsync(response).ConfigureAwait(false);
                    }

                    // set output property
                    dc.GetState().SetValue(this.Property, value);

                    return await dc.EndDialogAsync(value).ConfigureAwait(false);
                }
            }

            return await dc.EndDialogAsync().ConfigureAwait(false);
        }

        public override async Task<DialogTurnResult> ResumeDialogAsync(DialogContext dc, DialogReason reason, object result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await this.PromptUser(dc, InputState.Missing).ConfigureAwait(false);
        }

        protected abstract Task<InputState> OnRecognizeInput(DialogContext dc);

        protected override async Task<bool> OnPreBubbleEventAsync(DialogContext dc, DialogEvent e, CancellationToken cancellationToken)
        {
            if (e.Name == DialogEvents.ActivityReceived && dc.Context.Activity.Type == ActivityTypes.Message)
            {
                // Ask parent to perform recognition
                await dc.Parent.EmitEventAsync(AdaptiveEvents.RecognizeUtterance, value: dc.Context.Activity, bubble: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Should we allow interruptions
                var canInterrupt = true;
                if (allowInterruptions != null)
                {
                    var (value, error) = allowInterruptions.TryEvaluate(dc.GetState());
                    canInterrupt = error == null && value != null && (bool)value;
                }

                // Stop bubbling if interruptions ar NOT allowed
                return !canInterrupt;
            }

            return false;
        }

        protected IMessageActivity AppendChoices(IMessageActivity prompt, string channelId, IList<Choice> choices, ListStyle style, ChoiceFactoryOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get base prompt text (if any)
            var text = prompt != null && !string.IsNullOrEmpty(prompt.Text) ? prompt.Text : string.Empty;

            // Create temporary msg
            IMessageActivity msg;
            switch (style)
            {
                case ListStyle.Inline:
                    msg = ChoiceFactory.Inline(choices, text, null, options);
                    break;

                case ListStyle.List:
                    msg = ChoiceFactory.List(choices, text, null, options);
                    break;

                case ListStyle.SuggestedAction:
                    msg = ChoiceFactory.SuggestedAction(choices, text);
                    break;

                case ListStyle.HeroCard:
                    msg = ChoiceFactory.HeroCard(choices, text);
                    break;

                case ListStyle.None:
                    msg = Activity.CreateMessageActivity();
                    msg.Text = text;
                    break;

                default:
                    msg = ChoiceFactory.ForChannel(channelId, choices, text, null, options);
                    break;
            }

            // Update prompt with text, actions and attachments
            if (prompt != null)
            {
                // clone the prompt the set in the options (note ActivityEx has Properties so this is the safest mechanism)
                prompt = JsonConvert.DeserializeObject<Activity>(JsonConvert.SerializeObject(prompt));

                prompt.Text = msg.Text;

                if (msg.SuggestedActions != null && msg.SuggestedActions.Actions != null && msg.SuggestedActions.Actions.Count > 0)
                {
                    prompt.SuggestedActions = msg.SuggestedActions;
                }

                if (msg.Attachments != null && msg.Attachments.Any())
                {
                    prompt.Attachments = msg.Attachments;
                }

                return prompt;
            }
            else
            {
                msg.InputHint = InputHints.ExpectingInput;
                return msg;
            }
        }

        protected virtual object OnInitializeOptions(DialogContext dc, object options)
        {
            return options;
        }

        protected virtual async Task<IActivity> OnRenderPrompt(DialogContext dc, InputState state)
        {
            switch (state)
            {
                case InputState.Unrecognized:
                    if (this.UnrecognizedPrompt != null)
                    {
                        return await this.UnrecognizedPrompt.BindToData(dc.Context, dc.GetState()).ConfigureAwait(false);
                    }
                    else if (this.InvalidPrompt != null)
                    {
                        return await this.InvalidPrompt.BindToData(dc.Context, dc.GetState()).ConfigureAwait(false);
                    }

                    break;

                case InputState.Invalid:
                    if (this.InvalidPrompt != null)
                    {
                        return await this.InvalidPrompt.BindToData(dc.Context, dc.GetState()).ConfigureAwait(false);
                    }
                    else if (this.UnrecognizedPrompt != null)
                    {
                        return await this.UnrecognizedPrompt.BindToData(dc.Context, dc.GetState()).ConfigureAwait(false);
                    }

                    break;
            }

            return await this.Prompt.BindToData(dc.Context, dc.GetState()).ConfigureAwait(false);
        }

        private async Task<InputState> RecognizeInput(DialogContext dc, int turnCount)
        {
            dynamic input = null;

            // Use Property expression for input first
            if (!string.IsNullOrEmpty(this.Property))
            {
                dc.GetState().TryGetValue(this.Property, out input);

                // Clear property to avoid it being stuck on the next turn. It will get written 
                // back if the value passes validations.
                dc.GetState().SetValue(this.Property, null);
            }

            // Use Value expression for input second
            if (input == null && !string.IsNullOrEmpty(this.Value))
            {
                var (value, valueError) = new ExpressionEngine().Parse(this.Value).TryEvaluate(dc.GetState());
                if (valueError != null)
                { 
                    throw new Exception($"In InputDialog, this.Value expression evaluation resulted in an error. Expression: {this.Value}. Error: {valueError}");
                }
                 
                input = value;
            }

            // Fallback to using activity
            bool activityProcessed = dc.GetState().GetBoolValue(TurnPath.ACTIVITYPROCESSED);
            if (!activityProcessed && input == null && turnCount > 0)
            {
                if (this.GetType().Name == nameof(AttachmentInput))
                {
                    input = dc.Context.Activity.Attachments;
                }
                else
                {
                    input = dc.Context.Activity.Text;
                }
            }

            // Update "this.value" and perform additional recognition and validations
            dc.GetState().SetValue(VALUE_PROPERTY, input);
            if (input != null)
            {
                var state = await this.OnRecognizeInput(dc).ConfigureAwait(false);
                if (state == InputState.Valid)
                {
                    foreach (var validation in this.Validations)
                    {
                        var exp = new ExpressionEngine().Parse(validation);
                        var (value, error) = exp.TryEvaluate(dc.GetState());
                        if (value == null || (value is bool && (bool)value == false))
                        {
                            return InputState.Invalid;
                        }
                    }
                    
                    dc.GetState().SetValue(TurnPath.ACTIVITYPROCESSED, true);
                    return InputState.Valid;
                }
                else
                {
                    return state;
                }
            }
            else
            {
                return InputState.Missing;
            }
        }

        private async Task<DialogTurnResult> PromptUser(DialogContext dc, InputState state)
        {
            var prompt = await this.OnRenderPrompt(dc, state).ConfigureAwait(false);
            await dc.Context.SendActivityAsync(prompt).ConfigureAwait(false);
            return Dialog.EndOfTurn;
        }
    }
}
