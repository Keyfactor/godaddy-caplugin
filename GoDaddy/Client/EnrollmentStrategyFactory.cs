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
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace GoDaddy.Client;

public class EnrollmentStrategyFactory
{
    ILogger _logger = LogHandler.GetClassLogger<EnrollmentStrategyFactory>();
    private readonly IGoDaddyClient _client;
    private readonly ICertificateDataReader _certificateDataReader;

    public EnrollmentStrategyFactory(ICertificateDataReader certificateDataReader, IGoDaddyClient client)
    {
        _client = client;
        _certificateDataReader = certificateDataReader;
    }

    public async Task<IEnrollmentStrategy> GetStrategy(EnrollmentRequest request)
    {
        // In 99.9% of cases, EnrollmentType will be New or RenewOrReissue. If RenewOrReissue is specified,
        // we will use the EnrollmentRequest.PriorCertSN field to download the existing certificate and calculate
        // if a Renew or Reissue should be performed. 

        switch (request.EnrollmentType)
        {
            case EnrollmentType.New:
                _logger.LogTrace("Enrollment Strategy Factory - New Enrollment Strategy Selected");
                return new NewEnrollmentStrategy(_client);

            case EnrollmentType.Reissue:
                // Filter out Reissue requests - these should be handled by the RenewOrReissue strategy

            case EnrollmentType.Renew:
                // Filter out Renew requests - these should be handled by the RenewOrReissue strategy

            case EnrollmentType.RenewOrReissue:
                _logger.LogTrace("Enrollment Strategy Factory - Determining if Renew or Reissue Strategy is needed");
                return await RenewOrReissue(request);

            default:
                throw new ArgumentException($"Unsupported enrollment type: {request.EnrollmentType}");
        }
    }

    private async Task<IEnrollmentStrategy> RenewOrReissue(EnrollmentRequest request)
    {
        if (string.IsNullOrEmpty(request.PriorCertSN))
        {
            throw new ArgumentException("EnrollmentType is RenewOrReissue, but no PriorCertSN was provided.");
        }

        _logger.LogDebug($"Attempting to retrieve the certificate with serial number {request.PriorCertSN} from AnyGateway REST database");
        string certificateId = await _certificateDataReader.GetRequestIDBySerialNumber(request.PriorCertSN);
        if (string.IsNullOrEmpty(certificateId))
        {
            throw new Exception($"No certificate with serial number '{request.PriorCertSN}' could be found.");
        }

        DateTime? expirationDate = _certificateDataReader.GetExpirationDateByRequestId(certificateId);
        if (expirationDate == null)
        {
            _logger.LogDebug($"Couldn't retrieve expiration date for certificate with serial number {request.PriorCertSN} - getting certificate details from GoDaddy");
            CertificateDetailsRestResponse details = await _client.GetCertificateDetails(certificateId);
            if (details.validEnd == null)
            {
                throw new Exception($"Couldn't retrieve expiration date for certificate with serial number {request.PriorCertSN}");
            }
            expirationDate = details.validEnd;
        }

        // From GoDaddy - Renewal is the process by which the validity of a certificate is extended. Renewal 
        // is only available 60 days prior to expiration of the previous certificate and 30 days after the 
        // expiration of the previous certificate. The renewal supports modifying a set of the original certificate 
        // order information. Once a request is validated and approved, the certificate will be issued with 
        // extended validity.

        DateTime earliestRenewalDate = expirationDate.Value.AddDays(-60);
        DateTime latestRenewalDate = expirationDate.Value.AddDays(30);
        _logger.LogTrace($"Certificate with ID {certificateId} [serial number {request.PriorCertSN}] expires on {expirationDate}");
        _logger.LogTrace($"Earliest renewal date is {earliestRenewalDate}");
        _logger.LogTrace($"Latest renewal date is {latestRenewalDate}");

        if (DateTime.UtcNow < earliestRenewalDate)
        {
            _logger.LogDebug($"Certificate with serial number {request.PriorCertSN} is not yet eligible for renewal. Earliest renewal date is {earliestRenewalDate} - Reissuing instead.");
            return new ReissueEnrollmentStrategy(_client, certificateId);
        }

        if (DateTime.UtcNow > latestRenewalDate)
        {
            _logger.LogError($"Certificate with serial number {request.PriorCertSN} is no longer eligible for renewal. Latest renewal date was {latestRenewalDate}");
            throw new Exception($"Certificate with serial number {request.PriorCertSN} is no longer eligible for renewal. Latest renewal date was {latestRenewalDate}");
        }

        _logger.LogDebug($"Certificate with serial number {request.PriorCertSN} is eligible for renewal. Renewing certificate.");
        return new RenewEnrollmentStrategy(_client, certificateId);
    }
}

public class NewEnrollmentStrategy : IEnrollmentStrategy
{
    ILogger _logger = LogHandler.GetClassLogger<NewEnrollmentStrategy>();
    private IGoDaddyClient _client;

    public NewEnrollmentStrategy(IGoDaddyClient client)
    {
        _client = client;
    }

    public string StrategyName => "Enrollment";

    public async Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request, CancellationToken cancelToken)
    {
        _logger.LogDebug("NewEnrollmentStrategy - Preparing GoDaddy Certificate Order");

        // Map the EnrollmentRequest to a CertificateOrderRestRequest
        CertificateOrderRestRequest enrollmentRestRequest = new CertificateOrderRestRequest
        {
            Csr = request.CSR,
            CommonName = request.CommonName,
            Contact = new Contact
            {
                Email = request.Email,
                JobTitle = request.JobTitle,
                NameFirst = request.FirstName,
                NameLast = request.LastName,
                Phone = request.Phone,
            },
            Period = request.CertificateValidityInYears,
            ProductType = request.ProductType.ToString(),
            RootType = request.RootCAType.ToString(),
            SlotSize = request.SlotSize,
            SubjectAlternativeNames = request.SubjectAlternativeNames,
        };

        if (!request.ProductType.ToString().Contains("DV"))
        {
            _logger.LogDebug("NewEnrollmentStrategy - Adding Organization Information to the Certificate Order");
            enrollmentRestRequest.Organization = new Organization
            {
                Address = new Address
                {
                    Address1 = request.OrganizationAddress,
                    City = request.OrganizationCity,
                    Country = request.OrganizationCountry,
                    State = request.OrganizationState
                },
                Name = request.OrganizationName,
                Phone = request.OrganizationPhone,
            };
        }

        return await _client.Enroll(enrollmentRestRequest, cancelToken);
    }
}

public class ReissueEnrollmentStrategy : IEnrollmentStrategy
{
    ILogger _logger = LogHandler.GetClassLogger<ReissueEnrollmentStrategy>();
    private IGoDaddyClient _client;
    private string _certificateId;

    public ReissueEnrollmentStrategy(IGoDaddyClient client, string certificateId)
    {
        _client = client;
        _certificateId = certificateId;
    }

    public string StrategyName => "Reissue";

    public async Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request, CancellationToken cancelToken)
    {
        _logger.LogDebug("ReissueEnrollmentStrategy - Preparing GoDaddy Certificate Reissue");

        // Map the EnrollmentRequest to a ReissueCertificateRestRequest
        ReissueCertificateRestRequest reissueRestRequest = new ReissueCertificateRestRequest
        {
            Csr = request.CSR,
            CallbackUrl = "",
            CommonName = request.CommonName,
            RootType = request.RootCAType.ToString(),
            SubjectAlternativeNames = request.SubjectAlternativeNames,
        };

        return await _client.Reissue(_certificateId, reissueRestRequest, cancelToken);
    }
}

public class RenewEnrollmentStrategy : IEnrollmentStrategy
{
    ILogger _logger = LogHandler.GetClassLogger<RenewEnrollmentStrategy>();
    private IGoDaddyClient _client;
    private string _certificateId;

    public RenewEnrollmentStrategy(IGoDaddyClient client, string certificateId)
    {
        _client = client;
        _certificateId = certificateId;
    }

    public string StrategyName => "Renewal";

    public Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request, CancellationToken cancelToken)
    {
        _logger.LogDebug("RenewEnrollmentStrategy - Preparing GoDaddy Certificate Renewal");

        // Map the EnrollmentRequest to a RenewCertificateRestRequest
        RenewCertificateRestRequest renewRestRequest = new RenewCertificateRestRequest
        {
            Csr = request.CSR,
            CallbackUrl = "",
            CommonName = request.CommonName,
            Period = request.CertificateValidityInYears,
            RootType = request.RootCAType.ToString(),
            SubjectAlternativeNames = request.SubjectAlternativeNames,
        };

        return _client.Renew(_certificateId, renewRestRequest, cancelToken);
    }
}
