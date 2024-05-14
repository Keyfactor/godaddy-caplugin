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
using System.Net;
using System.Text.Json.Serialization;

namespace GoDaddy.Client;

// GET /v1/certificates/{certificateId}

[ApiResponse(HttpStatusCode.OK)] // 200 OK
public record CertificateDetailsRestResponse(
    string certificateId,
    string commonName,
    Contact contact,
    string createdAt,
    string deniedReason,
    Organization organization,
    int period,
    string productType,
    int progress,
    DateTime? revokedAt,
    string rootType,
    string serialNumber,
    string serialNumberHex,
    string slotSize,
    string status,
    SubjectAlternativeNames[] subjectAlternativeNames,
    DateTime? validEnd,
    DateTime? validStart
);

// GET /v1/certificates/{certificateId}/download

[ApiResponse(HttpStatusCode.OK)] // 200 OK
public record DownloadCertificateRestResponse(
    Pems pems,
    string serialNumber
);

// GET /v2/customers/{customerId}/certificates

[ApiResponse(HttpStatusCode.OK)] // 200 OK
public record CustomerCertificatesRestResponse(
    CertificateDetail[] certificates,
    Pagination pagination
);

// GET /v1/shoppers/{shopperId}

[ApiResponse(HttpStatusCode.OK)] // 200 OK
public record ShopperDetailsRestResponse(
    string customerId,
    string email,
    int? externalId,
    string marketId,
    string nameFirst,
    string nameLast,
    string shopperId
);

// POST /v1/certificates/validate

public record ValidateCertificateRestRequest(
    string callbackUrl,
    string commonName,
    Contact contact,
    string csr,
    bool intelVPro,
    Organization organization,
    int period,
    string productType,
    string rootType,
    string slotSize,
    string[] subjectAlternativeNames
);

[ApiResponse(HttpStatusCode.NoContent)] // 204 No Content
public record ValidateCertificateRestResponse();

// POST /v1/certificates

public enum CertificateEnrollmentType
{
    DV_SSL, // Domain Validated Secure Sockets Layer SSL certificate validated using domain name only
    DV_WILDCARD_SSL, // SSL certificate containing subdomains which is validated using domain name only
    EV_SSL, // Extended Validation SSL certificate validated using organization information, domain name, business legal status, and other factors
    OV_CS, // Code signing SSL certificate used by software developers to digitally sign apps. Validated using organization information
    OV_DS, // Driver signing SSL certificate used by software developers to digitally sign secure code for Windows hardware drivers. Validated using organization information
    OV_SSL, // SSL certificate validated using organization information and domain name
    OV_WILDCARD_SSL, // SSL certificate containing subdomains which is validated using organization information and domain name
    UCC_DV_SSL, // Unified Communication Certificate Multi domain SSL certificate validated using domain name only
    UCC_EV_SSL, // Multi domain SSL certificate validated using organization information, domain name, business legal status, and other factors
    UCC_OV_SSL // Multi domain SSL certificate validated using organization information and domain name
}

public enum RootCAType
{
    GODADDY_SHA_1, GODADDY_SHA_2, STARFIELD_SHA_1, STARFIELD_SHA_2
}

public enum SANSlotSize
{
    FIVE, TEN, FIFTEEN, TWENTY, THIRTY, FOURTY, FIFTY, ONE_HUNDRED
}

public record CertificateOrderRestRequest
{
    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; init; }

    [JsonPropertyName("commonName")]
    public string CommonName { get; init; }

    [JsonPropertyName("contact")]
    public Contact Contact { get; init; }

    [JsonPropertyName("csr")]
    public string Csr { get; init; }

    [JsonPropertyName("intelVPro")]
    public bool IntelVPro { get; init; }

    [JsonPropertyName("organization")]
    public Organization Organization { get; init; }

    [JsonPropertyName("period")]
    public int Period { get; init; }

    [JsonPropertyName("productType")]
    public string ProductType { get; init; }

    [JsonPropertyName("rootType")]
    public string RootType { get; init; }

    [JsonPropertyName("slotSize")]
    public string SlotSize { get; init; }

    [JsonPropertyName("subjectAlternativeNames")]
    public string[] SubjectAlternativeNames { get; init; }
};

[ApiResponse(HttpStatusCode.Accepted)] // 202 Accepted
public record CertificateOrderRestResponse(
    string certificateId
);

// POST /v1/certificates/reissue

public record ReissueCertificateRestRequest
{
    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; init; }

    [JsonPropertyName("commonName")]
    public string CommonName { get; init; }

    [JsonPropertyName("csr")]
    public string Csr { get; init; }

    [JsonPropertyName("delayExistingRevoke")]
    public int DelayExistingRevoke { get; init; }

    [JsonPropertyName("rootType")]
    public string RootType { get; init; }

    [JsonPropertyName("subjectAlternativeNames")]
    public string[] SubjectAlternativeNames { get; init; }

    [JsonPropertyName("forceDomainRevetting")]
    public string[] ForceDomainRevetting { get; init; }
}

[ApiResponse(HttpStatusCode.Accepted)] // 202 Accepted
public record ReissueCertificateRestResponse();

// POST /v1/certificates/renew

public record RenewCertificateRestRequest
{
    [JsonPropertyName("callbackUrl")]
    public string CallbackUrl { get; init; }

    [JsonPropertyName("commonName")]
    public string CommonName { get; init; }

    [JsonPropertyName("csr")]
    public string Csr { get; init; }

    [JsonPropertyName("period")]
    public int Period { get; init; }

    [JsonPropertyName("rootType")]
    public string RootType { get; init; }

    [JsonPropertyName("subjectAlternativeNames")]
    public string[] SubjectAlternativeNames { get; init; }
}

[ApiResponse(HttpStatusCode.Accepted)] // 202 Accepted
public record RenewCertificateRestResponse();
    
// POST /v1/certificates/revoke

public record RevokeCertificateRestRequest(
    string reason
);

public enum RevokeReason
{
    AFFILIATION_CHANGED, CESSATION_OF_OPERATION, KEY_COMPROMISE, PRIVILEGE_WITHDRAWN, SUPERSEDED
}

[ApiResponse(HttpStatusCode.NoContent)] // 204 No Content
public record RevokeCertificateRestResponse();

// POST /v1/certificates/{certificateId}/cancel

public record CancelCertificateOrderRestRequest();

[ApiResponse(HttpStatusCode.NoContent)] // 204 No Content
public record CancelCertificateOrderRestResponse();

// Common

public record Contact
{
    [JsonPropertyName("email")]
    public string Email { get; init; }

    [JsonPropertyName("jobTitle")]
    public string JobTitle { get; init; }

    [JsonPropertyName("nameFirst")]
    public string NameFirst { get; init; }

    [JsonPropertyName("nameLast")]
    public string NameLast { get; init; }

    [JsonPropertyName("nameMiddle")]
    public string NameMiddle { get; init; }

    [JsonPropertyName("phone")]
    public string Phone { get; init; }

    [JsonPropertyName("suffix")]
    public string Suffix { get; init; }
};

public record Organization
{
    [JsonPropertyName("address")]
    public Address Address { get; init; }

    [JsonPropertyName("assumedName")]
    public string AssumedName { get; init; }

    [JsonPropertyName("jurisdictionOfIncorporation")]
    public JurisdictionOfIncorporation JurisdictionOfIncorporation { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; }

    [JsonPropertyName("phone")]
    public string Phone { get; init; }

    [JsonPropertyName("registrationAgent")]
    public string RegistrationAgent { get; init; }

    [JsonPropertyName("registrationNumber")]
    public string RegistrationNumber { get; init; }
};

public record Address
{
    [JsonPropertyName("address1")]
    public string Address1 { get; init; }

    [JsonPropertyName("address2")]
    public string Address2 { get; init; }

    [JsonPropertyName("city")]
    public string City { get; init; }

    [JsonPropertyName("country")]
    public string Country { get; init; }

    [JsonPropertyName("postalCode")]
    public string PostalCode { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; }
};

public record JurisdictionOfIncorporation
{
    [JsonPropertyName("city")]
    public string City { get; init; }

    [JsonPropertyName("country")]
    public string Country { get; init; }

    [JsonPropertyName("county")]
    public string County { get; init; }

    [JsonPropertyName("state")]
    public string State { get; init; }
};

public record SubjectAlternativeNames(
    string status,
    string subjectAlternativeName
);

public record Pems(
    string certificate,
    string cross,
    string intermediate,
    string root
);

public record CertificateDetail(
    string certificateId,
    string commonName,
    int period,
    string type,
    string status,
    string createdAt,
    string completedAt,
    string validEndAt,
    string validStartAt,
    DateTime? revokedAt,
    bool renewalAvailable,
    string serialNumber,
    string slotSize,
    string[] subjectAlternativeNames
);

public record Pagination(
    string first,
    string previous,
    string next,
    string last,
    int total
);

// Errors

#nullable enable
public record Error(
    string code,
    ErrorField[]? fields,
    string message
);
#nullable restore

public record ErrorField(
    string code,
    string message,
    string path
);

