// Copyright 2024 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using GoDaddy.Client;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GoDaddy;

public class GoDaddyCAPluginBuilder<TBuilder> where TBuilder : IGoDaddyClientBuilder, new()
{
    public TBuilder _builder = new TBuilder();
    private ILogger _logger = LogHandler.GetClassLogger<GoDaddyCAPluginBuilder<TBuilder>>();

    public class ConfigConstants
    {
        public const string ApiKey = "ApiKey";
        public const string ApiSecret = "ApiSecret";
        public const string BaseUrl = "BaseUrl";
        public const string ShopperId = "ShopperId";
    }

    public class Config
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string BaseUrl { get; set; }
        public string ShopperId { get; set; }
    }

    public Dictionary<string, PropertyConfigInfo> GetPluginAnnotations()
    {
        return new Dictionary<string, PropertyConfigInfo>()
        {
            [ConfigConstants.ApiKey] = new PropertyConfigInfo()
            {
                Comments = "The API Key for the GoDaddy API",
                Hidden = true,
                DefaultValue = "",
                Type = "String"
            },
            [ConfigConstants.ApiSecret] = new PropertyConfigInfo()
            {
                Comments = "The API Secret for the GoDaddy API",
                Hidden = true,
                DefaultValue = "",
                Type = "String"
            },
            [ConfigConstants.BaseUrl] = new PropertyConfigInfo()
            {
                Comments = "The Base URL for the GoDaddy API - Usually either https://api.godaddy.com or https://api.ote-godaddy.com",
                Hidden = false,
                DefaultValue = "https://api.godaddy.com",
                Type = "String"
            },
            [ConfigConstants.ShopperId] = new PropertyConfigInfo()
            {
                Comments = "The Shopper ID of the GoDaddy account to use for the API calls (ex: 1234567890) - has a max length of 10 digits",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            }
        };
    }

    public GoDaddyCAPluginBuilder<TBuilder> WithConfigProvider(IAnyCAPluginConfigProvider configProvider)
    {
        _logger.LogDebug($"Builder - Setting values from Any CA Plugin Config Provider: {JsonConvert.SerializeObject(configProvider)}");

        string rawConfig = JsonConvert.SerializeObject(configProvider.CAConnectionData);
        Config properties = JsonConvert.DeserializeObject<Config>(rawConfig);

        _logger.LogTrace($"Builder - ApiKey: {properties.ApiKey}");
        _logger.LogTrace($"Builder - ApiSecret: {properties.ApiSecret}");
        _logger.LogTrace($"Builder - BaseUrl: {properties.BaseUrl}");
        _logger.LogTrace($"Builder - ShopperId: {properties.ShopperId}");

        _builder
            .WithApiKey(properties.ApiKey)
            .WithApiSecret(properties.ApiSecret)
            .WithBaseUrl(properties.BaseUrl)
            .WithShopperId(properties.ShopperId);

        return this;
    }

    public GoDaddyCAPluginBuilder<TBuilder> WithConnectionInformation(Dictionary<string, object> connectionInfo)
    {
        _logger.LogDebug($"Builder - Setting values from Connection Info: {JsonConvert.SerializeObject(connectionInfo)}");

        string rawConfig = JsonConvert.SerializeObject(connectionInfo);
        Config properties = JsonConvert.DeserializeObject<Config>(rawConfig);

        _logger.LogTrace($"Builder - ApiKey: {properties.ApiKey}");
        _logger.LogTrace($"Builder - ApiSecret: {properties.ApiSecret}");
        _logger.LogTrace($"Builder - BaseUrl: {properties.BaseUrl}");
        _logger.LogTrace($"Builder - ShopperId: {properties.ShopperId}");

        _builder
            .WithApiKey(properties.ApiKey)
            .WithApiSecret(properties.ApiSecret)
            .WithBaseUrl(properties.BaseUrl)
            .WithShopperId(properties.ShopperId);

        return this;
    }

    public GoDaddyCAPluginBuilder<TBuilder> WithLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    public IGoDaddyClient Build()
    {
        return _builder.Build();
    }
}
