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
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using static GoDaddy.GoDaddyCAPluginConfig;

namespace GoDaddy.Client;

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

                    // Extended Validation
                    EnrollmentConfigConstants.JurisdictionState,
                    EnrollmentConfigConstants.JurisdictionCountry,
                    EnrollmentConfigConstants.RegistrationNumber,

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

            var propertyInfo = typeof(EnrollmentRequest).GetProperty(parameter);
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(string))
            {
                propertyInfo.SetValue(_theEnrollmentRequest, productInfo.ProductParameters[parameter]);
            }
            else if (propertyInfo != null && propertyInfo.PropertyType == typeof(int))
            {
                if (int.TryParse(productInfo.ProductParameters[parameter], out int intValue))
                {
                    propertyInfo.SetValue(_theEnrollmentRequest, intValue);
                }
                else
                {
                    _logger.LogError($"Unable to parse integer value for product parameter: {parameter}");
                    throw new ArgumentException($"Unable to parse integer value for product parameter: {parameter}");
                }
            }
        }

        if (missingParameters.Count > 0)
        {
            _logger.LogError($"Missing required enrollment parameters:\n{string.Join("\n  - ", missingParameters)}");
            throw new ArgumentException($"Missing required enrollment parameters: {string.Join("\n  - ", missingParameters)}");
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