// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNetCore.Mvc.Formatters.Json.Internal
{
    /// <summary>
    /// Sets up JSON formatter options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcJsonMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly ILoggerFactory _loggerFactory;
        private JsonSerializerSettings _jsonSerializerSettings;
        private ArrayPool<char> _charPool;
        private ObjectPoolProvider _objectPoolProvider;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcJsonMvcOptionsSetup"/>.
        /// </summary>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <param name="jsonOptions"></param>
        /// <param name="charPool"></param>
        /// <param name="objectPoolProvider"></param>
        public MvcJsonMvcOptionsSetup(
            ILoggerFactory loggerFactory,
            IOptions<MvcJsonOptions> jsonOptions,
            ArrayPool<char> charPool,
            ObjectPoolProvider objectPoolProvider)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (jsonOptions == null)
            {
                throw new ArgumentNullException(nameof(jsonOptions));
            }

            _loggerFactory = loggerFactory;
            _jsonSerializerSettings = jsonOptions.Value.SerializerSettings;
            _charPool = charPool;
            _objectPoolProvider = objectPoolProvider;
        }

        public void Configure(MvcOptions options)
        {
            options.OutputFormatters.Add(new JsonOutputFormatter(_jsonSerializerSettings, _charPool));

            var jsonInputLogger = _loggerFactory.CreateLogger<JsonInputFormatter>();
            options.InputFormatters.Add(new JsonInputFormatter(
                jsonInputLogger,
                _jsonSerializerSettings,
                _charPool,
                _objectPoolProvider));

            var jsonInputPatchLogger = _loggerFactory.CreateLogger<JsonPatchInputFormatter>();
            options.InputFormatters.Add(new JsonPatchInputFormatter(
                jsonInputPatchLogger,
                _jsonSerializerSettings,
                _charPool,
                _objectPoolProvider));

            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));

            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(JToken)));
        }
    }
}
