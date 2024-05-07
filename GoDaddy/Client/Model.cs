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
    string revokedAt,
    string rootType,
    string serialNumber,
    string serialNumberHex,
    string slotSize,
    string status,
    SubjectAlternativeNames[] subjectAlternativeNames,
    string validEnd,
    string validStart
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

// POST /v1/certificates

public record CertificateOrderRestRequest(
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

[ApiResponse(HttpStatusCode.Accepted)] // 202 Accepted
public record CertificateOrderRestResponse(
    string certificateId
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

// POST /v1/certificates/reissue

public record ReissueCertificateRestRequest(
    string callbackUrl,
    string commonName,
    string csr,
    int delayExistingRevoke,
    string rootType,
    string[] subjectAlternativeNames,
    string[] forceDomainRevetting
);

[ApiResponse(HttpStatusCode.Accepted)] // 202 Accepted
public record ReissueCertificateRestResponse();

// POST /v1/certificates/renew

public record RenewCertificateRestRequest(
    string callbackUrl,
    string commonName,
    string csr,
    int period,
    string rootType,
    string[] subjectAlternativeNames
);

[ApiResponse(HttpStatusCode.Accepted)] // 202 Accepted
public record RenewCertificateRestResponse();
    
// POST /v1/certificates/revoke

public record RevokeCertificateRestRequest(
    string reason
);

[ApiResponse(HttpStatusCode.NoContent)] // 204 No Content
public record RevokeCertificateRestResponse();

// Common

public record Contact(
    string email,
    string jobTitle,
    string nameFirst,
    string nameLast,
    string nameMiddle,
    string phone,
    string suffix
);

public record Organization(
    Address address,
    string assumedName,
    JurisdictionOfIncorporation jurisdictionOfIncorporation,
    string name,
    string phone,
    string registrationAgent,
    string registrationNumber
);

public record Address(
    string address1,
    string address2,
    string city,
    string country,
    string postalCode,
    string state
);

public record JurisdictionOfIncorporation(
    string city,
    string country,
    string county,
    string state
);

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

public record Error(
    string code,
    ErrorFields[] fields,
    string message
);

public record ErrorFields(
    string code,
    string message,
    string path
);

