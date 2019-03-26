﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Expressions;

namespace Microsoft.Bot.Builder.Dialogs.Rules
{
    public interface IRule
    {
        /// <summary>
        /// Get the expression for this rule
        /// </summary>
        Expression GetExpression(PlanningContext context, DialogEvent dialogEvent);

        /// <summary>
        /// Execute the action for this rule
        /// </summary>
        /// <param name="context"></param>
        /// <param name="dialogEvent"></param>
        /// <returns></returns>
        Task<List<PlanChangeList>> ExecuteAsync(PlanningContext context);

        /// <summary>
        /// Steps to add to the plan when the rule is activated
        /// </summary>
        List<IDialog> Steps { get; }
    }
}
