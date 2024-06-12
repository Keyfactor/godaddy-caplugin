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
using System.Collections.Generic;
using System.Reflection;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using static Keyfactor.Extensions.CAPlugin.GoDaddy.GoDaddyCAPluginConfig;

namespace Keyfactor.Extensions.CAPlugin.GoDaddy.Client;

public class EnrollmentRequestBuilder : IEnrollmentRequestBuilder
{
    ILogger _logger = LogHandler.GetClassLogger<EnrollmentRequestBuilder>();
    private EnrollmentRequest _theEnrollmentRequest;

    public EnrollmentRequestBuilder()
    {
        _theEnrollmentRequest = new EnrollmentRequest();
    }

    public IEnrollmentRequestBuilder WithCsr(string csr)
    {
        _theEnrollmentRequest.CSR = csr;
        return this;
    }

    public IEnrollmentRequestBuilder WithEnrollmentProductInfo(EnrollmentProductInfo productInfo)
    {
        if (!Enum.TryParse<CertificateEnrollmentType>(productInfo.ProductID, out _theEnrollmentRequest.ProductType)) 
        {
            _logger.LogError($"Unable to parse product type from product ID: {productInfo.ProductID}");
            throw new ArgumentException($"Unable to parse product type from product ID: {productInfo.ProductID}");
        }
        _logger.LogDebug($"Product type: {_theEnrollmentRequest.ProductType}");

        // Parameter               | DV Required | OV Required | EV Required | Notes
        // ------------------------------------------------------------------------------------------------------------------------------------------------
        // callbackUrl             | No          | No          | No          | Required if client wants stateful actions via callback during certificate lifecycle.
        // commonName              | No          | No          | No          |
        // contact                 | Yes         | Yes         | Yes         |
        // contact.email           | Yes         | Yes         | Yes         |
        // contact.jobTitle        | No          | No          | Yes         | Only used for EVSSL.
        // contact.nameFirst       | Yes         | Yes         | Yes         |
        // contact.nameLast        | Yes         | Yes         | Yes         |
        // contact.nameMiddle      | No          | No          | No          |
        // contact.phone           | Yes         | Yes         | Yes         |
        // contact.suffix          | No          | No          | No          |
        // csr                     | Yes         | Yes         | Yes         |
        // intelVPro               | No          | Conditional | No          | Default is false. Only used for OV.
        // organization            | No          | Yes         | Yes         |
        // organization.address    | No          | Yes         | Yes         |
        // organization.assumedName| No          | No          | Yes         | Only for EVSSL.
        // organization.name       | No          | Yes         | Yes         |
        // organization.phone      | No          | Yes         | Yes         |
        // organization.registrationAgent | No   | No          | Yes         | Only for EVSSL.
        // organization.registrationNumber | No  | No          | Yes         | Only for EVSSL.
        // period                  | Yes         | Yes         | Yes         |
        // productType             | Yes         | Yes         | Yes         | Required for non-renewal.
        // rootType                | No          | No          | No          | Default is STARFIELD_SHA_2.
        // slotSize                | No          | No          | No          |
        // subjectAlternativeNames | No          | No          | No          | Unique items. Collection of subject alternative names to be included in certificate.

        List<string> requiredCustomParameters = new List<string>();
        switch (_theEnrollmentRequest.ProductType)
        {
            case CertificateEnrollmentType.DV_SSL:
            case CertificateEnrollmentType.DV_WILDCARD_SSL:
            case CertificateEnrollmentType.UCC_DV_SSL:
                requiredCustomParameters.AddRange(new string[] {
                    // Domain Validation
                    EnrollmentConfigConstants.Email,
                    EnrollmentConfigConstants.FirstName,
                    EnrollmentConfigConstants.LastName,
                    EnrollmentConfigConstants.Phone,

                    EnrollmentConfigConstants.RootCAType,
                    EnrollmentConfigConstants.CertificateValidityInYears,
                    EnrollmentConfigConstants.SlotSize,
                });
                break;

            // OV and OV Wildcard require the following custom parameters
            case CertificateEnrollmentType.OV_SSL:
            case CertificateEnrollmentType.OV_CS:
            case CertificateEnrollmentType.OV_DS:
            case CertificateEnrollmentType.OV_WILDCARD_SSL:
            case CertificateEnrollmentType.UCC_OV_SSL:
                requiredCustomParameters.AddRange(new string[] {
                    // Domain Validation
                    EnrollmentConfigConstants.Email,
                    EnrollmentConfigConstants.FirstName,
                    EnrollmentConfigConstants.LastName,
                    EnrollmentConfigConstants.Phone,

                    // Organization Validation
                    EnrollmentConfigConstants.OrganizationName,
                    EnrollmentConfigConstants.OrganizationAddress,
                    EnrollmentConfigConstants.OrganizationCity,
                    EnrollmentConfigConstants.OrganizationState,
                    EnrollmentConfigConstants.OrganizationCountry,
                    EnrollmentConfigConstants.OrganizationPhone,

                    EnrollmentConfigConstants.RootCAType,
                    EnrollmentConfigConstants.CertificateValidityInYears,
                    EnrollmentConfigConstants.SlotSize,
                });
                break;

            case CertificateEnrollmentType.EV_SSL:
            case CertificateEnrollmentType.UCC_EV_SSL:
                requiredCustomParameters.AddRange(new string[] {
                    // Domain Validation
                    EnrollmentConfigConstants.Email,
                    EnrollmentConfigConstants.FirstName,
                    EnrollmentConfigConstants.LastName,
                    EnrollmentConfigConstants.Phone,

                    // Organization Validation
                    EnrollmentConfigConstants.OrganizationName,
                    EnrollmentConfigConstants.OrganizationAddress,
                    EnrollmentConfigConstants.OrganizationCity,
                    EnrollmentConfigConstants.OrganizationState,
                    EnrollmentConfigConstants.OrganizationCountry,
                    EnrollmentConfigConstants.OrganizationPhone,

                    // Extended Validation
                    EnrollmentConfigConstants.RegistrationAgent,
                    EnrollmentConfigConstants.RegistrationNumber,
                    EnrollmentConfigConstants.JobTitle,

                    EnrollmentConfigConstants.RootCAType,
                    EnrollmentConfigConstants.CertificateValidityInYears,
                    EnrollmentConfigConstants.SlotSize,
                });
                break;

            default:
                _logger.LogError($"Invalid product type: {_theEnrollmentRequest.ProductType}");
                throw new ArgumentException($"Invalid product type: {_theEnrollmentRequest.ProductType}");
        }

        List<string> missingParameters = new List<string>();
        foreach (string parameter in requiredCustomParameters)
        {
            if (!productInfo.ProductParameters.ContainsKey(parameter) || string.IsNullOrEmpty(productInfo.ProductParameters[parameter]))
            {
                missingParameters.Add(parameter);
                continue;
            }

            _logger.LogDebug($"Adding product parameter: {parameter} = {productInfo.ProductParameters[parameter]}");

            var fieldInfo = typeof(EnrollmentRequest).GetField(parameter, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (fieldInfo != null && fieldInfo.FieldType == typeof(string))
            {
                _logger.LogTrace($"{fieldInfo.Name} is a string - setting value to {productInfo.ProductParameters[parameter]}");
                fieldInfo.SetValue(_theEnrollmentRequest, productInfo.ProductParameters[parameter]);
            }
            else if (fieldInfo != null && fieldInfo.FieldType == typeof(int))
            {
                if (int.TryParse(productInfo.ProductParameters[parameter], out int intValue))
                {
                    _logger.LogTrace($"{fieldInfo.Name} is an integer - setting value to {intValue}");
                    fieldInfo.SetValue(_theEnrollmentRequest, intValue);
                }
                else
                {
                    _logger.LogError($"Unable to parse integer value for product parameter: {parameter}");
                    throw new ArgumentException($"Unable to parse integer value for product parameter: {parameter}");
                }
            }
            else if (fieldInfo.FieldType.IsEnum)
            {
                if (Enum.TryParse(fieldInfo.FieldType, productInfo.ProductParameters[parameter], out var enumValue))
                {
                    _logger.LogTrace($"{fieldInfo.Name} is an enum - setting value to {enumValue}");
                    fieldInfo.SetValue(_theEnrollmentRequest, enumValue);
                }
                else
                {
                    _logger.LogError($"Unable to parse enum value for product parameter: {parameter} - valid values are: {string.Join(", ", Enum.GetNames(fieldInfo.FieldType))}");
                    throw new ArgumentException($"Unable to parse enum value for product parameter: {parameter} - valid values are: {string.Join(", ", Enum.GetNames(fieldInfo.FieldType))}");
                }
            }
            else if (fieldInfo == null)
            {
                _logger.LogError($"Failed to find field for product parameter: {parameter}");
                throw new ArgumentException($"Failed to find field for product parameter: {parameter}");
            }
            else
            {
                _logger.LogError($"Invalid field type for product parameter: {parameter}");
                throw new ArgumentException($"Invalid field type for product parameter: {parameter}");
            }
        }

        if (missingParameters.Count > 0)
        {
            _logger.LogError($"Missing required enrollment parameters:\n{string.Join("\n  - ", missingParameters)}");
            throw new ArgumentException($"Missing required enrollment parameters: {string.Join("\n  - ", missingParameters)}");
        }

        // Lastly, we try to retrieve PriorCertSN from ProductParameters. This field will be present if the
        // EnrollmentType is RenewOrReissue, but we won't fail the builder step if it's not present since
        // the absense of PriorCertSN is an AnyGateway REST error instead of a user-releated error.
        if (productInfo.ProductParameters.ContainsKey("PriorCertSN"))
        {
            _theEnrollmentRequest.PriorCertSN = productInfo.ProductParameters["PriorCertSN"];
        }

        return this;
    }

    public IEnrollmentRequestBuilder WithEnrollmentType(EnrollmentType enrollmentType)
    {
        _theEnrollmentRequest.EnrollmentType = enrollmentType;
        return this;
    }

    public IEnrollmentRequestBuilder WithRequestFormat(RequestFormat requestFormat)
    {
        // Unused 
        return this;
    }

    public IEnrollmentRequestBuilder WithSans(Dictionary<string, string[]> san)
    {
        List<string> sans = new List<string>();
        int count = 0;
        foreach (string[] sanValues in san.Values)
        {
            foreach (string sanValue in sanValues)
            {
                _logger.LogTrace($"Adding SAN: {sanValue}");
                sans.Add(sanValue);
                count++;
            }
        }
        _theEnrollmentRequest.SubjectAlternativeNames = sans.ToArray();
        _logger.LogDebug($"Added {count} SANs to the enrollment request");
        return this;
    }

    public IEnrollmentRequestBuilder WithSubject(string subject)
    {
        // Unused
        return this;
    }

    public EnrollmentRequest Build()
    {
        return _theEnrollmentRequest;
    }
}
