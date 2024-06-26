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
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.Extensions;

namespace Keyfactor.Extensions.CAPlugin.GoDaddy.Client;

public class EnrollmentRequest
{
    public CertificateEnrollmentType ProductType;
    public string CSR;
    public EnrollmentType EnrollmentType;
    public RootCAType RootCAType;
    public string[] SubjectAlternativeNames;
    public string CommonName;
    public bool IntelVPro;

    // Enrollment Config (specified by Workflow or Enrollment Parameters)
    public int CertificateValidityInYears;
    public string SlotSize;
    // DV
    public string LastName;
    public string FirstName;
    public string Email;
    public string Phone;
    // OV
    public string OrganizationName;
    public string OrganizationAddress;
    public string OrganizationCity;
    public string OrganizationState;
    public string OrganizationCountry;
    public string OrganizationPhone;
    // EV
    public string JobTitle;
    public string RegistrationAgent;
    public string RegistrationNumber;

    // AnyGateway REST config
    public string PriorCertSN;
}

public interface IEnrollmentRequestBuilder
{
    IEnrollmentRequestBuilder WithCsr(string csr);
    IEnrollmentRequestBuilder WithSubject(string subject);
    IEnrollmentRequestBuilder WithSans(Dictionary<string, string[]> san);
    IEnrollmentRequestBuilder WithEnrollmentProductInfo(EnrollmentProductInfo productInfo);
    IEnrollmentRequestBuilder WithRequestFormat(RequestFormat requestFormat);
    IEnrollmentRequestBuilder WithEnrollmentType(EnrollmentType enrollmentType);
    EnrollmentRequest Build();
}

public interface IEnrollmentStrategy
{
    string StrategyName { get; }
    Task<EnrollmentResult> ExecuteAsync(EnrollmentRequest request, CancellationToken cancelToken);
}
