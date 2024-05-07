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

public class GoDaddyCAPluginTests
{
    ILogger _logger { get; set;}

    public GoDaddyCAPluginTests()
    {
        ConfigureLogging();

        _logger = LogHandler.GetClassLogger<GoDaddyCAPluginTests>();
    }

    [Fact]
    public void GoDaddyCAPlugin_SynchronizeCAFull_ReturnSuccess()
    {
        // Arrange
        var testRevokeDate = DateTime.Now;
        IGoDaddyClient client = new FakeGoDaddyClient()
        {
            CertificatesIssuedByFakeGoDaddy = new Dictionary<string, AnyCAPluginCertificate>
            {
                { "test-cert-1", new AnyCAPluginCertificate
                    {
                        CARequestID = "fake-ca-request-id",
                        Certificate = "fake-certificate",
                        Status = 123,
                        ProductID = "fake-product-id",
                        RevocationDate = testRevokeDate,
                    }
                }
            }
        };

        BlockingCollection<AnyCAPluginCertificate> certificates = new BlockingCollection<AnyCAPluginCertificate>();
        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = client
        };

        // Act
        plugin.Synchronize(certificates, DateTime.Now, true, CancellationToken.None).Wait();

        // Assert
        Assert.Equal("fake-ca-request-id", certificates.First().CARequestID);
        Assert.Equal("fake-certificate", certificates.First().Certificate);
        Assert.Equal(123, certificates.First().Status);
        Assert.Equal("fake-product-id", certificates.First().ProductID);
        Assert.Equal(testRevokeDate, certificates.First().RevocationDate);
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

