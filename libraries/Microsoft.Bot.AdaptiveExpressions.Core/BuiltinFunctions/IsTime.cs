﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.AdaptiveExpressions.Core.Memory;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace Microsoft.Bot.AdaptiveExpressions.Core.BuiltinFunctions
{
    /// <summary>
    /// Return true if a given TimexProperty or Timex expression refers to a valid time.
    /// Valid time contains hours, minutes and seconds.
    /// </summary>
    internal class IsTime : ExpressionEvaluator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IsTime"/> class.
        /// </summary>
        public IsTime()
            : base(ExpressionType.IsTime, Evaluator, ReturnType.Boolean, FunctionUtils.ValidateUnary)
        {
        }

        private static (object value, string error) Evaluator(Expression expression, IMemory state, Options options)
        {
            TimexProperty parsed = null;
            bool? value = null;
            string error = null;
            IReadOnlyList<object> args;
            (args, error) = FunctionUtils.EvaluateChildren(expression, state, options);
            if (error == null)
            {
                (parsed, error) = FunctionUtils.ParseTimexProperty(args[0]);
            }

            if (error == null)
            {
                value = parsed.Hour != null && parsed.Minute != null && parsed.Second != null;
            }

            return (value, error);
        }
    }
}
