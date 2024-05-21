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
            IGoDaddyClient client = new FakeGoDaddyClient();

            return client;
        }
    }

    public FakeGoDaddyClient()
    {
        EnrollmentNotBefore = DateTime.UtcNow;
        EnrollmentNotAfter = DateTime.UtcNow.AddYears(1);
    }

    ILogger _logger = LogHandler.GetClassLogger<FakeGoDaddyClient>();

    public Dictionary<string, AnyCAPluginCertificate>? CertificatesIssuedByFakeGoDaddy { get; set; }

    public DateTime EnrollmentNotBefore;
    public DateTime EnrollmentNotAfter;

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

        if (CertificatesIssuedByFakeGoDaddy.ContainsKey(certificateId))
            return Task.FromResult(CertificatesIssuedByFakeGoDaddy[certificateId]);

        throw new Exception($"Certificate with ID {certificateId} not found");
    }

    public Task<string> DownloadCertificatePem(string certificateId)
    {
        if (CertificatesIssuedByFakeGoDaddy == null)
        {
            throw new Exception("No certificates have been issued by Fake GoDaddy - no items set");
        }

        if (CertificatesIssuedByFakeGoDaddy.ContainsKey(certificateId))
            return Task.FromResult(CertificatesIssuedByFakeGoDaddy[certificateId].Certificate);

        throw new Exception($"Certificate with ID {certificateId} not found");
    }

    public Task RevokeCertificate(string certificateId, RevokeReason reason)
    {
        return Task.CompletedTask;
    }

    public Task CancelCertificateOrder(string certificateId)
    {
        throw new NotImplementedException();
    }

    public Task<EnrollmentResult> Enroll(CertificateOrderRestRequest request, CancellationToken cancelToken)
    {
        // Validate that request has the correct fields for the product type
        switch (request.ProductType)
        {
            case "DV_SSL":
            case "DV_WILDCARD_SSL":
            case "UCC_DV_SSL":
                Assert.NotNull(request.Contact);
                Assert.NotNull(request.Contact.Email);
                Assert.NotNull(request.Contact.NameFirst);
                Assert.NotNull(request.Contact.NameLast);
                Assert.NotNull(request.Contact.Phone);

                Assert.NotNull(request.Csr);
                Assert.NotNull(request.Period);
                Assert.NotNull(request.SlotSize);
                break;

            case "OV_SSL":
            case "OV_CS":
            case "OV_DS":
            case "OV_WILDCARD_SSL":
            case "UCC_OV_SSL":
                Assert.NotNull(request.Contact);
                Assert.NotNull(request.Contact.Email);
                Assert.NotNull(request.Contact.NameFirst);
                Assert.NotNull(request.Contact.NameLast);
                Assert.NotNull(request.Contact.Phone);

                Assert.NotNull(request.Organization);
                Assert.NotNull(request.Organization.Name);
                Assert.NotNull(request.Organization.Address);
                Assert.NotNull(request.Organization.Address.City);
                Assert.NotNull(request.Organization.Address.State);
                Assert.NotNull(request.Organization.Address.Country);
                Assert.NotNull(request.Organization.Phone);

                Assert.NotNull(request.Csr);
                Assert.NotNull(request.Period);
                Assert.NotNull(request.SlotSize);
                break;

            case "EV_SSL":
            case "UCC_EV_SSL":
                Assert.NotNull(request.Contact);
                Assert.NotNull(request.Contact.Email);
                Assert.NotNull(request.Contact.NameFirst);
                Assert.NotNull(request.Contact.NameLast);
                Assert.NotNull(request.Contact.Phone);

                Assert.NotNull(request.Organization);
                Assert.NotNull(request.Organization.Name);
                Assert.NotNull(request.Organization.Address);
                Assert.NotNull(request.Organization.Address.City);
                Assert.NotNull(request.Organization.Address.State);
                Assert.NotNull(request.Organization.Address.Country);
                Assert.NotNull(request.Organization.Phone);

                Assert.NotNull(request.Organization.RegistrationAgent);
                Assert.NotNull(request.Organization.RegistrationNumber);
                Assert.NotNull(request.Contact.JobTitle);

                Assert.NotNull(request.Csr);
                Assert.NotNull(request.Period);
                Assert.NotNull(request.SlotSize);
                break;

            default:
                _logger.LogError($"Invalid product type: {request.ProductType}");
                throw new ArgumentException($"Invalid product type: {request.ProductType}");
        }
        _logger.LogInformation("Enrolling certificate with Fake GoDaddy");
        X509Certificate2 cert = SignFakeCsr(request.Csr);

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

    public Task<EnrollmentResult> Reissue(string certificateId, ReissueCertificateRestRequest request, CancellationToken cancelToken)
    {
        _logger.LogInformation("Reissuing certificate with Fake GoDaddy");
        X509Certificate2 cert = SignFakeCsr(request.Csr);

        _logger.LogDebug("Preparing EnrollmentResult object");
        EnrollmentResult result = new EnrollmentResult
        {
            CARequestID = certificateId,
            Status = (int)EndEntityStatus.GENERATED,
            StatusMessage = $"Certificate with ID {certificateId} has been reissued",
            Certificate = cert.ExportCertificatePem()
        };

        return Task.FromResult(result);
    }

    public Task<EnrollmentResult> Renew(string certificateId, RenewCertificateRestRequest request, CancellationToken cancelToken)
    {
        _logger.LogInformation("Renewing certificate with Fake GoDaddy");
        X509Certificate2 cert = SignFakeCsr(request.Csr);

        _logger.LogDebug("Preparing EnrollmentResult object");
        EnrollmentResult result = new EnrollmentResult
        {
            CARequestID = certificateId,
            Status = (int)EndEntityStatus.GENERATED,
            StatusMessage = $"Certificate with ID {certificateId} has been renewed",
            Certificate = cert.ExportCertificatePem()
        };

        return Task.FromResult(result);
    }

    private X509Certificate2 SignFakeCsr(string csrString)
    {
        _logger.LogDebug("Serializing csr string to CertificateRequest object");
        CertificateRequest csr = CertificateRequest.LoadSigningRequestPem(
            csrString.AsSpan(),
            HashAlgorithmName.SHA256,
            CertificateRequestLoadOptions.Default,
            RSASignaturePadding.Pkcs1
        );

        _logger.LogDebug("Generating self-signed CA certificate");
        using var caKeyPair = RSA.Create(2048);
        var caCertificate = GenerateSelfSignedCertificate(caKeyPair, "CN=Test CA", DateTime.UtcNow, DateTime.UtcNow.AddYears(1));

        var serialNumber = new byte[8];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(serialNumber);
        }

        _logger.LogDebug($"Creating certificate from CSR valid from {EnrollmentNotBefore} to {EnrollmentNotAfter}");
        X509Certificate2 cert = csr.Create(
            caCertificate.SubjectName,
            X509SignatureGenerator.CreateForRSA(caKeyPair, RSASignaturePadding.Pkcs1),
            EnrollmentNotBefore,
            EnrollmentNotAfter,
            serialNumber
        );

        return cert;
    }

    public static X509Certificate2 GenerateSelfSignedCertificate(RSA keyPair, string subjectName, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var request = new CertificateRequest(
            new X500DistinguishedName(subjectName),
            keyPair,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1
        );
        var cert = request.CreateSelfSigned(notBefore, notAfter);
        return cert;
    }
}
