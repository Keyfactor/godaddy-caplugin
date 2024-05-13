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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using GoDaddy.Client;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Keyfactor.PKI.Enums.EJBCA;
using Microsoft.Extensions.Logging;

namespace GoDaddy.Tests;

public class FakeGoDaddyClient : IGoDaddyClient
{

    public class Builder : IGoDaddyClientBuilder
    {
        private string? _baseUrl { get; set; }
        private string? _apiKey { get; set; }
        private string? _apiSecret { get; set; }
        private string? _shopperId { get; set; }
        
        public IGoDaddyClientBuilder WithApiKey(string apiToken)
        {
            _apiKey = apiToken;
            return this;
        }

        public IGoDaddyClientBuilder WithApiSecret(string apiSecret)
        { 
            _apiSecret = apiSecret;
            return this;
        }

        public IGoDaddyClientBuilder WithBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl;
            return this;
        }

        public IGoDaddyClientBuilder WithShopperId(string shopperId) {
            _shopperId = shopperId;
            return this;
        }

        public IGoDaddyClient Build()
        {
            IGoDaddyClient client = new GoDaddyClient(_baseUrl, _apiKey, _apiSecret, _shopperId);

            return client;
        }
    }

    ILogger _logger = LogHandler.GetClassLogger<FakeGoDaddyClient>();

    public Dictionary<string, AnyCAPluginCertificate>? CertificatesIssuedByFakeGoDaddy { get; set; }

    public Task Ping()
    {
        return Task.CompletedTask;
    }

    public Task<string> DownloadCertificate(string certificateId)
    {
        throw new NotImplementedException();
    }

    public Task<int> DownloadAllIssuedCertificates(BlockingCollection<AnyCAPluginCertificate> certificatesBuffer, CancellationToken cancelToken)
    {
        _logger.LogDebug("Getting all issued certificates from Fake GoDaddy");

        if (CertificatesIssuedByFakeGoDaddy == null)
        {
            throw new Exception("No certificates have been issued by Fake GoDaddy - no items set");
        }

        foreach (var cert in CertificatesIssuedByFakeGoDaddy)
        {
            certificatesBuffer.Add(cert.Value);
        }

        return Task.FromResult(CertificatesIssuedByFakeGoDaddy.Count);
    }

    public Task<CertificateDetailsRestResponse> GetCertificateDetails(string certificateId)
    {
        throw new NotImplementedException();
    }

    Task<AnyCAPluginCertificate> IGoDaddyClient.DownloadCertificate(string certificateId)
    {
        if (CertificatesIssuedByFakeGoDaddy == null)
        {
            throw new Exception("No certificates have been issued by Fake GoDaddy - no items set");
        }

        if (CertificatesIssuedByFakeGoDaddy.TryGetValue(certificateId, out AnyCAPluginCertificate certificate))
            return Task.FromResult(certificate);

        throw new Exception($"Certificate with ID {certificateId} not found");
    }

    public Task<string> DownloadCertificatePem(string certificateId)
    {
        if (CertificatesIssuedByFakeGoDaddy == null)
        {
            throw new Exception("No certificates have been issued by Fake GoDaddy - no items set");
        }

        if (CertificatesIssuedByFakeGoDaddy.TryGetValue(certificateId, out AnyCAPluginCertificate certificate))
            return Task.FromResult(certificate.Certificate);

        throw new Exception($"Certificate with ID {certificateId} not found");
    }

    public Task<EnrollmentResult> Enroll(CertificateOrderRestRequest request)
    {
        _logger.LogInformation("Enrolling certificate with Fake GoDaddy");
        _logger.LogDebug("Serializing csr string to CertificateRequest object");
        CertificateRequest csr = CertificateRequest.LoadSigningRequestPem(
            request.Csr.AsSpan(),
            HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.Default,
            RSASignaturePadding.Pkcs1
        );

        _logger.LogDebug("Generating self-signed CA certificate");
        using var caKeyPair = RSA.Create(2048);
        var caCertificate = GenerateSelfSignedCertificate(caKeyPair, "CN=Test CA");

        var serialNumber = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(serialNumber);
        }

        _logger.LogDebug("Creating certificate from CSR");
        X509Certificate2 cert = csr.Create(
            caCertificate.SubjectName,
            X509SignatureGenerator.CreateForRSA(caKeyPair, RSASignaturePadding.Pkcs1),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddYears(1),
            serialNumber
        );

        _logger.LogDebug("Preparing EnrollmentResult object");
        EnrollmentResult result = new EnrollmentResult
        {
            CARequestID = Guid.NewGuid().ToString(),
            Status = (int)EndEntityStatus.GENERATED,
            StatusMessage = "Certificate generated successfully",
            Certificate = cert.ExportCertificatePem()
        };

        return Task.FromResult(result);
    }

    public static X509Certificate2 GenerateSelfSignedCertificate(RSA keyPair, string subjectName)
    {
        var request = new CertificateRequest(
            new X500DistinguishedName(subjectName),
            keyPair,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        var cert = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddYears(1));
        return cert;
    }
}
