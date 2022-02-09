﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.AI.QnA
{
    /// <summary>
    /// Helper class for Generate Answer API.
    /// </summary>
    internal class GenerateAnswerUtils
    {
        private readonly HttpClient _httpClient;
        private readonly IBotTelemetryClient _telemetryClient;
        private readonly QnAMakerEndpoint _endpoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateAnswerUtils"/> class.
        /// </summary>
        /// <param name="telemetryClient">Telemetry client.</param>
        /// <param name="endpoint">QnA Maker endpoint details.</param>
        /// <param name="options">QnA Maker options.</param>
        /// <param name="httpClient">Http client.</param>
        public GenerateAnswerUtils(IBotTelemetryClient telemetryClient, QnAMakerEndpoint endpoint, QnAMakerOptions options, HttpClient httpClient)
        {
            _telemetryClient = telemetryClient;
            _endpoint = endpoint;

            Options = options ?? new QnAMakerOptions();
            ValidateOptions(Options);
            _httpClient = httpClient;
        }

        /// <summary>
        /// Gets or sets qnA Maker options.
        /// </summary>
        /// <value>The options for QnAMaker.</value>
        public QnAMakerOptions Options { get; set; }

        /// <summary>
        /// Generates an answer from the knowledge base.
        /// </summary>
        /// <param name="turnContext">The Turn Context that contains the user question to be queried against your knowledge base.</param>
        /// <param name="messageActivity">Message activity of the turn context.</param>
        /// <param name="options">The options for the QnA Maker knowledge base. If null, constructor option is used for this instance.</param>
        /// <returns>A list of answers for the user query, sorted in decreasing order of ranking score.</returns>
        [Obsolete]
        public async Task<Collection<QueryResult>> GetAnswersAsync(ITurnContext turnContext, IMessageActivity messageActivity, QnAMakerOptions options)
        {
            var result = await GetAnswersRawAsync(turnContext, messageActivity, options).ConfigureAwait(false);

            return result.Answers;
        }

        /// <summary>
        /// Generates an answer from the knowledge base.
        /// </summary>
        /// <param name="turnContext">The Turn Context that contains the user question to be queried against your knowledge base.</param>
        /// <param name="messageActivity">Message activity of the turn context.</param>
        /// <param name="options">The options for the QnA Maker knowledge base. If null, constructor option is used for this instance.</param>
        /// <returns>A list of answers for the user query, sorted in decreasing order of ranking score.</returns>
        public async Task<QueryResults> GetAnswersRawAsync(ITurnContext turnContext, IMessageActivity messageActivity, QnAMakerOptions options)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (turnContext.Activity == null)
            {
                throw new ArgumentException($"The {nameof(turnContext.Activity)} property for {nameof(turnContext)} can't be null.", nameof(turnContext));
            }

            if (messageActivity == null)
            {
                throw new ArgumentException("Activity type is not a message");
            }

            var hydratedOptions = HydrateOptions(options);
            ValidateOptions(hydratedOptions);

            var result = await QueryQnaServiceAsync((Activity)messageActivity, hydratedOptions).ConfigureAwait(false);

            await EmitTraceInfoAsync(turnContext, (Activity)messageActivity, result.Answers.ToArray(), hydratedOptions).ConfigureAwait(false);

            return result;
        }

        private static async Task<QueryResults> FormatQnaResultAsync(HttpResponseMessage response, QnAMakerOptions options)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            var results = JsonConvert.DeserializeObject<QueryResults>(jsonResponse);

            foreach (var answer in results.Answers)
            {
                answer.Score = answer.Score / 100;
            }

            results.SetAnswers(results.Answers.Where(answer => answer.Score > options.ScoreThreshold).ToArray());

            return results;
        }

        private static void ValidateOptions(QnAMakerOptions options)
        {
            if (options.ScoreThreshold == 0)
            {
                options.ScoreThreshold = 0.3F;
            }

            if (options.Top == 0)
            {
                options.Top = 1;
            }

            if (options.ScoreThreshold < 0 || options.ScoreThreshold > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options), $"The {nameof(options.ScoreThreshold)} property should be a value between 0 and 1");
            }

            if (options.Timeout == 0.0D)
            {
                options.Timeout = 100000;
            }

            if (options.Top < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(options), $"The {nameof(options.Top)} property should be an integer greater than 0");
            }

            if (options.StrictFilters == null)
            {
                options.SetStrictFilters(Array.Empty<Metadata>());
            }

            if (options.RankerType == null)
            {
                options.RankerType = RankerTypes.DefaultRankerType;
            }
        }

        /// <summary>
        /// Combines QnAMakerOptions passed into the QnAMaker constructor with the options passed as arguments into GetAnswersAsync().
        /// </summary>
        /// <param name="queryOptions">The options for the QnA Maker knowledge base.</param>
        /// <returns>Return modified options for the QnA Maker knowledge base.</returns>
        private QnAMakerOptions HydrateOptions(QnAMakerOptions queryOptions)
        {
            var hydratedOptions = JsonConvert.DeserializeObject<QnAMakerOptions>(JsonConvert.SerializeObject(Options));

            if (queryOptions != null)
            {
                if (queryOptions.ScoreThreshold != hydratedOptions.ScoreThreshold && queryOptions.ScoreThreshold != 0)
                {
                    hydratedOptions.ScoreThreshold = queryOptions.ScoreThreshold;
                }

                if (queryOptions.Top != hydratedOptions.Top && queryOptions.Top != 0)
                {
                    hydratedOptions.Top = queryOptions.Top;
                }

                if (queryOptions.StrictFilters?.Count > 0)
                {
                    hydratedOptions.SetStrictFilters(queryOptions.StrictFilters.ToArray());
                }

                hydratedOptions.Context = queryOptions.Context;
                hydratedOptions.QnAId = queryOptions.QnAId;
                hydratedOptions.IsTest = queryOptions.IsTest;
                hydratedOptions.RankerType = queryOptions.RankerType != null ? queryOptions.RankerType : RankerTypes.DefaultRankerType;
                hydratedOptions.StrictFiltersJoinOperator = queryOptions.StrictFiltersJoinOperator;
            }

            return hydratedOptions;
        }

        private async Task<QueryResults> QueryQnaServiceAsync(Activity messageActivity, QnAMakerOptions options)
        {
            var requestUrl = $"{_endpoint.Host}/knowledgebases/{_endpoint.KnowledgeBaseId}/generateanswer";
            var jsonRequest = JsonConvert.SerializeObject(
                new
                {
                    question = messageActivity.Text,
                    top = options.Top,
                    strictFilters = options.StrictFilters,
                    scoreThreshold = options.ScoreThreshold,
                    context = options.Context,
                    qnaId = options.QnAId,
                    isTest = options.IsTest,
                    rankerType = options.RankerType,
                    StrictFiltersCompoundOperationType = options.StrictFiltersJoinOperator,
                }, Formatting.None);

            var httpRequestHelper = new HttpRequestUtils(_httpClient);
            var response = await httpRequestHelper.ExecuteHttpRequestAsync(requestUrl, jsonRequest, _endpoint).ConfigureAwait(false);

            var result = await FormatQnaResultAsync(response, options).ConfigureAwait(false);

            return result;
        }

        private async Task EmitTraceInfoAsync(ITurnContext turnContext, Activity messageActivity, QueryResult[] result, QnAMakerOptions options)
        {
            var traceInfo = new QnAMakerTraceInfo
            {
                Message = messageActivity,
                QueryResults = result,
                KnowledgeBaseId = _endpoint.KnowledgeBaseId,
                ScoreThreshold = options.ScoreThreshold,
                Top = options.Top,
                StrictFilters = options.StrictFilters.ToArray(),
                Context = options.Context,
                QnAId = options.QnAId,
                IsTest = options.IsTest,
                RankerType = options.RankerType
            };
            var traceActivity = Activity.CreateTraceActivity(QnAMaker.QnAMakerName, QnAMaker.QnAMakerTraceType, traceInfo, QnAMaker.QnAMakerTraceLabel);
            await turnContext.SendActivityAsync(traceActivity).ConfigureAwait(false);
        }
    }
}
