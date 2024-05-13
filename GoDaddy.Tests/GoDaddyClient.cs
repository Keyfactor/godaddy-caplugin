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

using System.Collections.Concurrent;
using GoDaddy.Client;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace GoDaddy.Tests;

public class ClientTests
{
    ILogger _logger { get; set;}

    public ClientTests()
    {
        ConfigureLogging();

        _logger = LogHandler.GetClassLogger<ClientTests>();
    }

    [IntegrationTestingFact]
    public void GoDaddyClient_DownloadAllIssuedCerts_ReturnSuccess()
    {
        // Arrange
        IntegrationTestingFact env = new();

        IGoDaddyClient client = new GoDaddyClient.Builder()
            .WithBaseUrl(env.BaseApiUrl)
            .WithApiKey(env.ApiKey)
            .WithApiSecret(env.ApiSecret)
            .WithShopperId(env.ShopperId)
            .Build();

        BlockingCollection<AnyCAPluginCertificate> certificates = new();

        // Act
        int numberOfDownloadedCerts = client.DownloadAllIssuedCertificates(certificates, CancellationToken.None).Result;
        _logger.LogInformation($"Number of downloaded certificates: {numberOfDownloadedCerts}");
    }

    [IntegrationTestingFact]
    public void GoDaddyClient_GetCertificateDetails_ReturnSuccess()
    {
        // Arrange
        IntegrationTestingFact env = new();

        IGoDaddyClient client = new GoDaddyClient.Builder()
            .WithBaseUrl(env.BaseApiUrl)
            .WithApiKey(env.ApiKey)
            .WithApiSecret(env.ApiSecret)
            .WithShopperId(env.ShopperId)
            .Build();

        BlockingCollection<AnyCAPluginCertificate> certificates = new();

        // Act
        int numberOfDownloadedCerts = client.DownloadAllIssuedCertificates(certificates, CancellationToken.None).Result;
        _logger.LogInformation($"Number of downloaded certificates: {numberOfDownloadedCerts}");
    }

    static void ConfigureLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();

        // Targets where to log to: File and Console
        var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
        logconsole.Layout = @"${date:format=HH\:mm\:ss} ${logger} [${level}] - ${message}";

        // Rules for mapping loggers to targets            
        config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logconsole);

        // Apply config           
        NLog.LogManager.Configuration = config;

        LogHandler.Factory = LoggerFactory.Create(builder =>
                {
                builder.AddNLog();
                });
    }
}
