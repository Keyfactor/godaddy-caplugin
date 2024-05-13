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

namespace GoDaddy.Client;

public class EnrollmentStrategyFactory
{
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
                return new NewEnrollmentStrategy(_client);
            case EnrollmentType.Reissue:
                return new ReissueEnrollmentStrategy(_client);
            case EnrollmentType.Renew:
                return new RenewEnrollmentStrategy(_client);
            default:
                throw new ArgumentException($"Invalid enrollment type: {request.EnrollmentType}");
        }
    }
}

public class NewEnrollmentStrategy : IEnrollmentStrategy
{
    private IGoDaddyClient client;

    public NewEnrollmentStrategy(IGoDaddyClient client)
    {
        this.client = client;
    }

    public async Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request)
    {
        // Map the EnrollmentRequest to a CertificateOrderRestRequest
        CertificateOrderRestRequest enrollmentRestRequest = new CertificateOrderRestRequest
        {
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
    }
}

public class ReissueEnrollmentStrategy : IEnrollmentStrategy
{
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
