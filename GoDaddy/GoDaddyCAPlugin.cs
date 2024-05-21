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
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Extensions.CAPlugin.GoDaddy.Client;
using Keyfactor.Logging;
using Keyfactor.PKI.Enums.EJBCA;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.CAPlugin.GoDaddy;

public class GoDaddyCAPlugin : IAnyCAPlugin
{
    public IGoDaddyClient Client { get; set; }
    ILogger _logger = LogHandler.GetClassLogger<GoDaddyCAPlugin>();
    ICertificateDataReader _certificateDataReader;

    private const int _certificateIssuanceTimeoutSeconds = 600;

    public void Initialize(IAnyCAPluginConfigProvider configProvider, ICertificateDataReader certificateDataReader)
    {
        if (Client == null)
        {
            GoDaddyCAPluginBuilder<GoDaddyClient.Builder> builder = new();
            Client = builder
                .WithConfigProvider(configProvider)
                .Build();

            if (builder.IsGoDaddyPluginEnabled()) Client.Enable();
            else Client.Disable();

            _logger.LogDebug("Created GoDaddy API Client");
        }
        else
        {
            _logger.LogDebug("GoDaddy API Client already initialized");
        }

        _certificateDataReader = certificateDataReader;

        _logger.LogDebug("GoDaddyCAPlugin initialized");
    }

    public async Task ValidateCAConnectionInfo(Dictionary<string, object> connectionInfo)
    {
        if (Client == null)
        {
            GoDaddyCAPluginBuilder<GoDaddyClient.Builder> builder = new();
            Client = builder
                .WithConnectionInformation(connectionInfo)
                .Build();

            if (builder.IsGoDaddyPluginEnabled()) await Client.Enable();
            else await Client.Disable();
            _logger.LogDebug("Created GoDaddy API Client");
        }
        else
        {
            _logger.LogDebug("GoDaddy API Client already initialized");
        }

        await Ping();
    }

    public Task ValidateProductInfo(EnrollmentProductInfo productInfo, Dictionary<string, object> connectionInfo)
    {
        // WithEnrollmentProductInfo() validates that the custom parameters in EnrollmentProductInfo are valid
        new EnrollmentRequestBuilder().WithEnrollmentProductInfo(productInfo);
        // If this method doesn't throw, the product info is valid
        return Task.CompletedTask;
    }

    public async Task Ping()
    {
        if (!Client.IsEnabled())
        {
            _logger.LogDebug("GoDaddyCAPlugin is disabled. Skipping Ping");
            return;
        }
        ValidateClient();
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
        ValidateClient();

        if (_certificateDataReader == null)
        {
            throw new Exception("CertificateDataReader is not initialized. Please call Initialize() first.");
        }

        EnrollmentRequest request = new EnrollmentRequestBuilder()
            .WithCsr(csr)
            .WithSubject(subject)
            .WithSans(san)
            .WithEnrollmentProductInfo(productInfo)
            .WithRequestFormat(requestFormat)
            .WithEnrollmentType(enrollmentType)
            .Build();

        EnrollmentStrategyFactory factory = new EnrollmentStrategyFactory(_certificateDataReader, Client);
        IEnrollmentStrategy strategy = await factory.GetStrategy(request);

        using (CancellationTokenSource tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_certificateIssuanceTimeoutSeconds)))
        {
            CancellationToken token = tokenSource.Token;
            try
            {
                return await strategy.ExecuteAsync(request, token);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning("Enrollment task was cancelled: " + ex.Message);
                return null;
            }
        }
    }

    public async Task<AnyCAPluginCertificate> GetSingleRecord(string caRequestID)
    {
        ValidateClient();
        _logger.LogDebug($"Getting certificate with request ID: {caRequestID}");
        return await Client.DownloadCertificate(caRequestID);
    }

    public async Task<int> Revoke(string caRequestID, string hexSerialNumber, uint revocationReason)
    {
        ValidateClient();
        _logger.LogDebug($"Revoking certificate with request ID: {caRequestID}");
        RevokeReason reason = RevokeReason.CESSATION_OF_OPERATION;
        switch (revocationReason)
        {
            case 1:
                // Key Compromise
                reason = RevokeReason.KEY_COMPROMISE;
                break;

            case 2:
                // CA Compromise
                reason = RevokeReason.KEY_COMPROMISE;
                break;

            case 3:
                // Affiliation Changed
                reason = RevokeReason.AFFILIATION_CHANGED;
                break;

            case 4:
                // Superseded
                reason = RevokeReason.SUPERSEDED;
                break;

            case 5:
                // Cessation of Operation
                reason = RevokeReason.CESSATION_OF_OPERATION;
                break;

            case 6:
                // Certificate Hold
                reason = RevokeReason.PRIVILEGE_WITHDRAWN;
                break;

            case 8:
                // Remove from CRL
                reason = RevokeReason.PRIVILEGE_WITHDRAWN;
                break;

            default:
                reason = RevokeReason.CESSATION_OF_OPERATION;
                break;
        }

        await Client.RevokeCertificate(caRequestID, reason);

        return (int)EndEntityStatus.REVOKED;
    }

    public async Task Synchronize(BlockingCollection<AnyCAPluginCertificate> blockingBuffer, DateTime? lastSync, bool fullSync, CancellationToken cancelToken)
    {
        ValidateClient();
        
        _logger.LogInformation("Performing a full CA synchronization");
        int certificates = await Client.DownloadAllIssuedCertificates(blockingBuffer, cancelToken);
        _logger.LogDebug($"Synchronized {certificates} certificates");
    }

    private void ValidateClient()
    {
        if (Client == null)
        {
            throw new Exception("GoDaddyCAPlugin is not initialized. Please call Initialize() first.");
        }
    }
}
