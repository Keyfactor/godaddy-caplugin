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
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Extensions.CAPlugin.GoDaddy.Client;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Keyfactor.Extensions.CAPlugin.GoDaddy;

public class GoDaddyCAPluginBuilder<TBuilder> where TBuilder : IGoDaddyClientBuilder, new()
{
    public TBuilder _builder = new TBuilder();
    private ILogger _logger = LogHandler.GetClassLogger<GoDaddyCAPluginBuilder<TBuilder>>();
    private bool _enabled = false;

    public GoDaddyCAPluginBuilder<TBuilder> WithConfigProvider(IAnyCAPluginConfigProvider configProvider)
    {
        _logger.LogDebug($"Builder - Setting values from Any CA Plugin Config Provider");

        string rawConfig = JsonConvert.SerializeObject(configProvider.CAConnectionData);
        GoDaddyCAPluginConfig.Config properties = JsonConvert.DeserializeObject<GoDaddyCAPluginConfig.Config>(rawConfig);

        _logger.LogTrace($"Builder - ApiKey: {properties.ApiKey}");
        _logger.LogTrace($"Builder - ApiSecret: {properties.ApiSecret}");
        _logger.LogTrace($"Builder - BaseUrl: {properties.BaseUrl}");
        _logger.LogTrace($"Builder - ShopperId: {properties.ShopperId}");
        _logger.LogTrace($"Builder - Enabled: {properties.Enabled}");

        _builder
            .WithApiKey(properties.ApiKey)
            .WithApiSecret(properties.ApiSecret)
            .WithBaseUrl(properties.BaseUrl)
            .WithShopperId(properties.ShopperId);
        
        _enabled = properties.Enabled;

        return this;
    }

    public GoDaddyCAPluginBuilder<TBuilder> WithConnectionInformation(Dictionary<string, object> connectionInfo)
    {
        _logger.LogDebug($"Builder - Setting values from Connection Info");

        string rawConfig = JsonConvert.SerializeObject(connectionInfo);
        GoDaddyCAPluginConfig.Config properties = JsonConvert.DeserializeObject<GoDaddyCAPluginConfig.Config>(rawConfig);

        _logger.LogTrace($"Builder - ApiKey: {properties.ApiKey}");
        _logger.LogTrace($"Builder - ApiSecret: {properties.ApiSecret}");
        _logger.LogTrace($"Builder - BaseUrl: {properties.BaseUrl}");
        _logger.LogTrace($"Builder - ShopperId: {properties.ShopperId}");
        _logger.LogTrace($"Builder - Enabled: {properties.Enabled}");

        _builder
            .WithApiKey(properties.ApiKey)
            .WithApiSecret(properties.ApiSecret)
            .WithBaseUrl(properties.BaseUrl)
            .WithShopperId(properties.ShopperId);

        _enabled = properties.Enabled;

        return this;
    }

    public GoDaddyCAPluginBuilder<TBuilder> WithLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    public IGoDaddyClient Build()
    {
        IGoDaddyClient client = _builder.Build();
        if (_enabled) client.Enable();
        else client.Disable();
        return client;
    }
}
