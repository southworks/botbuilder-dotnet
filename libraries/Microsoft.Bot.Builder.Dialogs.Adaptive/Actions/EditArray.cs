﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Expressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Actions
{
    /// <summary>
    /// Lets you modify an array in memory.
    /// </summary>
    public class EditArray : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.EditArray";

        private Expression value;
        private Expression itemsProperty;
        private Expression resultProperty;

        /// <summary>
        /// Initializes a new instance of the <see cref="EditArray"/> class.
        /// </summary>
        /// <param name="changeType">change type.</param>
        /// <param name="arrayProperty">array property (optional).</param>
        /// <param name="value">value to insert.</param>
        /// <param name="resultProperty">output property to put Pop/Take into.</param>
        public EditArray(ArrayChangeType changeType, string arrayProperty = null, string value = null, string resultProperty = null)
            : base()
        {
            this.ChangeType = changeType;

            if (!string.IsNullOrEmpty(arrayProperty))
            {
                this.ItemsProperty = arrayProperty;
            }

            switch (changeType)
            {
                case ArrayChangeType.Clear:
                case ArrayChangeType.Pop:
                case ArrayChangeType.Take:
                    this.ResultProperty = resultProperty;
                    break;
                case ArrayChangeType.Push:
                case ArrayChangeType.Remove:
                    this.Value = value;
                    break;
            }
        }

        [JsonConstructor]
        public EditArray([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ArrayChangeType
        {
            /// <summary>
            /// Push item onto the end of the array
            /// </summary>
            Push,

            /// <summary>
            /// Pop the item off the end of the array
            /// </summary>
            Pop,

            /// <summary>
            /// Take an item from the front of the array
            /// </summary>
            Take,

            /// <summary>
            /// Remove the item from the array, regardless of it's location
            /// </summary>
            Remove,

            /// <summary>
            /// Clear the contents of the array
            /// </summary>
            Clear
        }

        /// <summary>
        /// Gets or sets type of change being applied.
        /// </summary>
        /// <value>
        /// Type of change being applied.
        /// </value>
        [JsonProperty("changeType")]
        public ArrayChangeType ChangeType { get; set; }

        /// <summary>
        /// Gets or sets property path expression to the collection of items.
        /// </summary>
        /// <value>
        /// Property path expression to the collection of items.
        /// </value>
        [JsonProperty("itemsProperty")]
        public string ItemsProperty
        {
            get { return itemsProperty?.ToString(); }
            set { this.itemsProperty = (value != null) ? new ExpressionEngine().Parse(value) : null; }
        }

        /// <summary>
        /// Gets or sets the path expression to store the result of the action.
        /// </summary>
        /// <value>
        /// The path expression to store the result of the action.
        /// </value>
        [JsonProperty("resultProperty")]
        public string ResultProperty
        {
            get { return resultProperty?.ToString(); }
            set { this.resultProperty = (value != null) ? new ExpressionEngine().Parse(value) : null; }
        }

        /// <summary>
        /// Gets or sets the expression of the value to put onto the array.
        /// </summary>
        /// <value>
        /// The expression of the value to put onto the array.
        /// </value>
        [JsonProperty("value")]
        public string Value
        {
            get { return value?.ToString(); }
            set { this.value = (value != null) ? new ExpressionEngine().Parse(value) : null; }
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (string.IsNullOrEmpty(ItemsProperty))
            {
                throw new Exception($"EditArray: \"{ChangeType}\" operation couldn't be performed because the arrayProperty wasn't specified.");
            }

            var array = dc.GetState().GetValue<JArray>(this.ItemsProperty, () => new JArray());

            object item = null;
            object result = null;

            switch (ChangeType)
            {
                case ArrayChangeType.Pop:
                    item = array[array.Count - 1];
                    array.RemoveAt(array.Count - 1);
                    result = item;
                    break;
                case ArrayChangeType.Push:
                    EnsureValue();
                    var (itemResult, error) = this.value.TryEvaluate(dc.GetState());
                    if (error == null && itemResult != null)
                    {
                        array.Add(itemResult);
                    }

                    break;
                case ArrayChangeType.Take:
                    if (array.Count == 0)
                    {
                        break;
                    }

                    item = array[0];
                    array.RemoveAt(0);
                    result = item;
                    break;
                case ArrayChangeType.Remove:
                    EnsureValue();
                    (itemResult, error) = this.value.TryEvaluate(dc.GetState());
                    if (error == null && itemResult != null)
                    {
                        result = false;
                        for (var i = 0; i < array.Count(); ++i)
                        {
                            if (array[i].ToString() == itemResult.ToString() || JToken.DeepEquals(array[i], JToken.FromObject(itemResult)))
                            {
                                result = true;
                                array.RemoveAt(i);
                                break;
                            }
                        }
                    }

                    break;
                case ArrayChangeType.Clear:
                    result = array.Count > 0;
                    array.Clear();
                    break;
            }

            dc.GetState().SetValue(this.ItemsProperty, array);

            if (ResultProperty != null)
            {
                dc.GetState().SetValue(this.ResultProperty, result);
            }

            return await dc.EndDialogAsync(result);
        }

        protected override string OnComputeId()
        {
            return $"{this.GetType().Name}[{ChangeType + ": " + ItemsProperty}]";
        }

        private void EnsureValue()
        {
            if (Value == null)
            {
                throw new Exception($"EditArray: \"{ChangeType}\" operation couldn't be performed for array \"{ItemsProperty}\" because a value wasn't specified.");
            }
        }
    }
}
