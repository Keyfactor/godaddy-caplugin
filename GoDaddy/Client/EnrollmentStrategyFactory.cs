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
using System.Threading.Tasks;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace GoDaddy.Client;

public class EnrollmentStrategyFactory
{
    ILogger _logger = LogHandler.GetClassLogger<EnrollmentStrategyFactory>();
    private readonly IGoDaddyClient _client;

    public EnrollmentStrategyFactory(IGoDaddyClient client)
    {
        _client = client;
    }

    public IEnrollmentStrategy GetStrategy(EnrollmentRequest request)
    {
        switch (request.EnrollmentType)
        {
            case EnrollmentType.New:
                _logger.LogTrace("Enrollment Strategy Factory - New Enrollment Strategy Selected");
                return new NewEnrollmentStrategy(_client);
            case EnrollmentType.Reissue:
                _logger.LogTrace("Enrollment Strategy Factory - Reissue Enrollment Strategy Selected");
                return new ReissueEnrollmentStrategy(_client);
            case EnrollmentType.Renew:
                _logger.LogTrace("Enrollment Strategy Factory - Renew Enrollment Strategy Selected");
                return new RenewEnrollmentStrategy(_client);
            default:
                throw new ArgumentException($"Invalid enrollment type: {request.EnrollmentType}");
        }
    }
}

public class NewEnrollmentStrategy : IEnrollmentStrategy
{
    ILogger _logger = LogHandler.GetClassLogger<NewEnrollmentStrategy>();
    private IGoDaddyClient client;

    public NewEnrollmentStrategy(IGoDaddyClient client)
    {
        this.client = client;
    }

    public async Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request)
    {
        _logger.LogDebug("NewEnrollmentStrategy - Preparing GoDaddy Certificate Order");

        // Map the EnrollmentRequest to a CertificateOrderRestRequest
        CertificateOrderRestRequest enrollmentRestRequest = new CertificateOrderRestRequest
        {
            Csr = request.CSR,
            CallbackUrl = "",
            CommonName = request.CommonName,
            Contact = new Contact
            {
                Email = request.Email,
                JobTitle = request.JobTitle,
                NameFirst = request.FirstName,
                NameLast = request.LastName,
                NameMiddle = "",
                Phone = request.Phone,
                Suffix = ""
            },
            Organization = new Organization
            {
                Address = new Address
                {
                    Address1 = request.OrganizationAddress,
                    Address2 = "",
                    City = request.OrganizationCity,
                    Country = request.OrganizationCountry,
                    PostalCode = "",
                    State = request.OrganizationState
                },
                Name = request.OrganizationName,
                Phone = request.OrganizationPhone,
            },
            Period = request.CertificateValidityInYears,
            ProductType = request.ProductType.ToString(),
            RootType = request.RootCAType.ToString(),
            SlotSize = request.SlotSize,
            SubjectAlternativeNames = request.SubjectAlternativeNames,
        };

        return await client.Enroll(enrollmentRestRequest);
    }
}

public class ReissueEnrollmentStrategy : IEnrollmentStrategy
{
    ILogger _logger = LogHandler.GetClassLogger<ReissueEnrollmentStrategy>();
    private IGoDaddyClient client;

    public ReissueEnrollmentStrategy(IGoDaddyClient client)
    {
        this.client = client;
    }

    public async Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request)
    {
        throw new System.NotImplementedException();
    }
}

public class RenewEnrollmentStrategy : IEnrollmentStrategy
{
    ILogger _logger = LogHandler.GetClassLogger<RenewEnrollmentStrategy>();
    private IGoDaddyClient client;

    public RenewEnrollmentStrategy(IGoDaddyClient client)
    {
        this.client = client;
    }

    public async Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request)
    {
        throw new System.NotImplementedException();
    }
}
