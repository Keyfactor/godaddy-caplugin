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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GoDaddy.Client;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace GoDaddy;

public class GoDaddyCAPlugin : IAnyCAPlugin
{
    public IGoDaddyClient Client { get; set; }
    ILogger _logger = LogHandler.GetClassLogger<GoDaddyCAPlugin>();

    public void Initialize(IAnyCAPluginConfigProvider configProvider, ICertificateDataReader certificateDataReader)
    {
        if (Client == null)
        {
            Client = new GoDaddyCAPluginBuilder<GoDaddyClient.Builder>()
                .WithConfigProvider(configProvider)
                .Build();

            _logger.LogDebug("Created GoDaddy API Client");
        }

        _logger.LogDebug("GoDaddyCAPlugin initialized");
    }

    public async Task ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
    {
        if (Client == null)
        {
            Client = new GoDaddyCAPluginBuilder<GoDaddyClient.Builder>()
                .WithConnectionInformation(connectionInfo)
                .Build();

            _logger.LogDebug("Created GoDaddy API Client");
        }

        await Ping();
    }

    public async Task ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
    {
        if (Client == null)
        {
            Client = new GoDaddyCAPluginBuilder<GoDaddyClient.Builder>()
                .WithConnectionInformation(connectionInfo)
                .Build();

            _logger.LogDebug("Created GoDaddy API Client");
        }

        await Ping();
    }

    public async Task Ping()
    {
        InternalValidate();
        _logger.LogDebug("Pinging GoDaddy API to validate connection");
        await Client.Ping();
    }

    public Dictionary<string, PropertyConfigInfo> GetCAConnectorAnnotations()
    {
        _logger.LogDebug("Getting CA Connector Annotations");
        return GoDaddyCAPluginConfig.GetPluginAnnotations();
    }

    public Dictionary<string, PropertyConfigInfo> GetTemplateParameterAnnotations()
    {
        _logger.LogDebug("Getting Template Parameter Annotations");
        return GoDaddyCAPluginConfig.GetTemplateParameterAnnotations();
    }

    public List<string> GetProductIds()
    {
        _logger.LogDebug("Returning available Product IDs");
        return new List<string>(Enum.GetNames(typeof(CertificateEnrollmentType)));
    }

    public async Task<EnrollmentResult> Enroll(string csr, string subject, Dictionary<string, string[]> san, EnrollmentProductInfo productInfo, RequestFormat requestFormat, EnrollmentType enrollmentType)
    {
        _logger.LogDebug($"csr: {csr} subject: {subject} san: {san} requestFormat: {requestFormat} enrollmentType: {enrollmentType}");
        foreach (var sanField in san)
        {
            _logger.LogDebug($"SAN: {sanField.Key} - {string.Join(", ", sanField.Value)}");
        }
        foreach (var productField in productInfo.ProductParameters)
        {
            _logger.LogDebug($"Product Field: {productField.Key} - {productField.Value}");
        }

        InternalValidate();
        EnrollmentStrategyFactory factory = new EnrollmentStrategyFactory(Client);

        EnrollmentRequest request = new EnrollmentRequestBuilder()
            .WithCsr(csr)
            .WithSubject(subject)
            .WithSans(san)
            .WithEnrollmentProductInfo(productInfo)
            .WithRequestFormat(requestFormat)
            .WithEnrollmentType(enrollmentType)
            .Build();

        IEnrollmentStrategy strategy = factory.GetStrategy(request);
        return await strategy.ExecuteAsync(request);
    }

    public async Task<AnyCAPluginCertificate> GetSingleRecord(string caRequestID)
    {
        InternalValidate();
        _logger.LogDebug($"Getting certificate with request ID: {caRequestID}");
        return await Client.DownloadCertificate(caRequestID);
    }

    public async Task<int> Revoke(string caRequestID, string hexSerialNumber, uint revocationReason)
    {
        throw new NotImplementedException();
    }

    public async Task Synchronize(BlockingCollection<AnyCAPluginCertificate> blockingBuffer, DateTime? lastSync, bool fullSync, CancellationToken cancelToken)
    {
        InternalValidate();
        
        if (fullSync)
        {
            _logger.LogInformation("Performing a full CA synchronization");
            int certificates = await Client.DownloadAllIssuedCertificates(blockingBuffer, cancelToken);
            _logger.LogDebug($"Synchronized {certificates} certificates");
        }
    }

    private void InternalValidate()
    {
        if (Client == null)
        {
            throw new Exception("GoDaddyCAPlugin is not initialized. Please call Initialize() first.");
        }
    }
}
