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
using Keyfactor.AnyGateway.Extensions;

namespace GoDaddy;

public class GoDaddyCAPluginConfig
{
    public class ConfigConstants
    {
        public const string ApiKey = "ApiKey";
        public const string ApiSecret = "ApiSecret";
        public const string BaseUrl = "BaseUrl";
        public const string ShopperId = "ShopperId";
        public const string Enabled = "Enabled";
    }

    public class Config
    {
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }
        public string BaseUrl { get; set; }
        public string ShopperId { get; set; }
        public bool Enabled { get; set; }
    }

    public static class EnrollmentConfigConstants
    {
        public const string JobTitle = "JobTitle";
        public const string CertificateValidityInYears = "CertificateValidityInYears";
        public const string LastName = "LastName";
        public const string FirstName = "FirstName";
        public const string Email = "Email";
        public const string Phone = "Phone";
        public const string SlotSize = "SlotSize";
        public const string OrganizationName = "OrganizationName";
        public const string OrganizationAddress = "OrganizationAddress";
        public const string OrganizationCity = "OrganizationCity";
        public const string OrganizationState = "OrganizationState";
        public const string OrganizationCountry = "OrganizationCountry";
        public const string OrganizationPhone = "OrganizationPhone";
        public const string RegistrationAgent = "RegistrationAgent";
        public const string RegistrationNumber = "RegistrationNumber";
    }

    public static Dictionary<string, PropertyConfigInfo> GetPluginAnnotations()
    {
        return new Dictionary<string, PropertyConfigInfo>()
        {
            [ConfigConstants.ApiKey] = new PropertyConfigInfo()
            {
                Comments = "The API Key for the GoDaddy API",
                         Hidden = true,
                         DefaultValue = "",
                         Type = "String"
            },
            [ConfigConstants.ApiSecret] = new PropertyConfigInfo()
            {
                Comments = "The API Secret for the GoDaddy API",
                Hidden = true,
                DefaultValue = "",
                Type = "String"
            },
            [ConfigConstants.BaseUrl] = new PropertyConfigInfo()
            {
                Comments = "The Base URL for the GoDaddy API - Usually either https://api.godaddy.com or https://api.ote-godaddy.com",
                Hidden = false,
                DefaultValue = "https://api.godaddy.com",
                Type = "String"
            },
            [ConfigConstants.ShopperId] = new PropertyConfigInfo()
            {
                Comments = "The Shopper ID of the GoDaddy account to use for the API calls (ex: 1234567890) - has a max length of 10 digits",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [ConfigConstants.Enabled] = new PropertyConfigInfo()
            {
                Comments = "Flag to Enable or Disable gateway functionality. Disabling is primarily used to allow creation of the CA prior to configuration information being available.",
                Hidden = false,
                DefaultValue = true,
                Type = "Boolean"
            }
        };
    }

    public static Dictionary<string, PropertyConfigInfo> GetTemplateParameterAnnotations()
    {
        return new Dictionary<string, PropertyConfigInfo>()
        {
            [EnrollmentConfigConstants.JobTitle] = new PropertyConfigInfo()
            {
                Comments = "The job title of the certificate requestor",
                Hidden = false,
                DefaultValue = "",
                Type = "String",
            },
            [EnrollmentConfigConstants.CertificateValidityInYears] = new PropertyConfigInfo()
            {
                Comments = "Number of years the certificate will be valid for",
                Hidden = false,
                DefaultValue = "1",
                Type = "Number"
            },
            [EnrollmentConfigConstants.LastName] = new PropertyConfigInfo()
            {
                Comments = "Last name of the certificate requestor",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.FirstName] = new PropertyConfigInfo()
            {
                Comments = "First name of the certificate requestor",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.Email] = new PropertyConfigInfo()
            {
                Comments = "Email address of the requestor",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.Phone] = new PropertyConfigInfo()
            {
                Comments = "Phone number of the requestor",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.SlotSize] = new PropertyConfigInfo()
            {
                Comments = "Maximum number of SANs that a certificate may have - valid values are [FIVE, TEN, FIFTEEN, TWENTY, THIRTY, FOURTY, FIFTY, ONE_HUNDRED]",
                Hidden = false,
                DefaultValue = "FIVE",
                Type = "String",
            },
            [EnrollmentConfigConstants.OrganizationName] = new PropertyConfigInfo()
            {
                Comments = "Name of the organization to be validated against",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.OrganizationAddress] = new PropertyConfigInfo()
            {
                Comments = "Address of the organization to be validated against",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.OrganizationCity] = new PropertyConfigInfo()
            {
                Comments = "City of the organization to be validated against",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.OrganizationState] = new PropertyConfigInfo()
            {
                Comments = "Full state name of the organization to be validated against",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.OrganizationCountry] = new PropertyConfigInfo()
            {
                Comments = "2 character abbreviation of the country of the organization to be validated against",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.OrganizationPhone] = new PropertyConfigInfo()
            {
                Comments = "Phone number of the organization to be validated against",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.RegistrationAgent] = new PropertyConfigInfo()
            {
                Comments = "Registration agent name assigned to the organization when its documents were filed for registration",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            },
            [EnrollmentConfigConstants.RegistrationNumber] = new PropertyConfigInfo()
            {
                Comments = "Registration number assigned to the organization when its documents were filed for registration",
                Hidden = false,
                DefaultValue = "",
                Type = "String"
            }
        };
    }
}
