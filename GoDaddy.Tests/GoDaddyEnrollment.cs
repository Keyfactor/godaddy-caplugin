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
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using static GoDaddy.GoDaddyCAPluginConfig;

namespace GoDaddy.Tests;

public class EnrollmentAbstractionTests
{
    ILogger _logger { get; set;}

    public EnrollmentAbstractionTests()
    {
        ConfigureLogging();

        _logger = LogHandler.GetClassLogger<EnrollmentAbstractionTests>();
    }

    [IntegrationTestingFact]
    public void EnrollmentBuilder_ValidParameters_ReturnSuccess()
    {
        // Arrange
        string subject = "CN=example.com";
        string fakeCsrString = GenerateCSR("CN=example.com,O=Example Company,L=Example City,ST=Example State,C=US");
        Dictionary<string, string[]> sans = new Dictionary<string, string[]>
        {
            { "DNS", new string[] { "example.com", "www.example.com" } },
            { "IP", new string[] { "192.168.1.1" } },
        };
        EnrollmentProductInfo productInfo = new EnrollmentProductInfo
        {
            ProductID = "EV_SSL",
            ProductParameters = new Dictionary<string, string>
            {
                { EnrollmentConfigConstants.JobTitle, "Software Engineer" },
                { EnrollmentConfigConstants.CertificateValidityInYears, "2" },
                { EnrollmentConfigConstants.LastName, "Doe" },
                { EnrollmentConfigConstants.FirstName, "John" },
                { EnrollmentConfigConstants.Email, "john.doe@example.com" },
                { EnrollmentConfigConstants.Phone, "123-456-7890" },
                { EnrollmentConfigConstants.SlotSize, "1024" },
                { EnrollmentConfigConstants.OrganizationName, "Example Corp" },
                { EnrollmentConfigConstants.OrganizationAddress, "1234 Elm Street" },
                { EnrollmentConfigConstants.OrganizationCity, "Example City" },
                { EnrollmentConfigConstants.OrganizationState, "EX" },
                { EnrollmentConfigConstants.OrganizationCountry, "USA" },
                { EnrollmentConfigConstants.OrganizationPhone, "987-654-3210" },
                { EnrollmentConfigConstants.JurisdictionState, "EX" },
                { EnrollmentConfigConstants.JurisdictionCountry, "USA" },
                { EnrollmentConfigConstants.RegistrationNumber, "REG-12345" }
            }
        };
        RequestFormat requestFormat = RequestFormat.PKCS10;
        EnrollmentType enrollmentType = EnrollmentType.New;

        // Act
        EnrollmentRequest request = new EnrollmentRequestBuilder()
            .WithCsr(fakeCsrString)
            .WithSubject(subject)
            .WithSans(sans)
            .WithEnrollmentProductInfo(productInfo)
            .WithRequestFormat(requestFormat)
            .WithEnrollmentType(enrollmentType)
            .Build();

        _logger.LogDebug($"{JsonConvert.SerializeObject(request)}");

        // Assert
        Assert.Equal("Software Engineer", request.JobTitle);
        Assert.Equal(2, request.CertificateValidityInYears);
        Assert.Equal("Doe", request.LastName);
        Assert.Equal("John", request.FirstName);
        Assert.Equal("john.doe@example.com", request.Email);
        Assert.Equal("123-456-7890", request.Phone);
        Assert.Equal("1024", request.SlotSize);
        Assert.Equal("Example Corp", request.OrganizationName);
        Assert.Equal("1234 Elm Street", request.OrganizationAddress);
        Assert.Equal("Example City", request.OrganizationCity);
        Assert.Equal("EX", request.OrganizationState);
        Assert.Equal("USA", request.OrganizationCountry);
        Assert.Equal("987-654-3210", request.OrganizationPhone);
        Assert.Equal("EX", request.JurisdictionState);
        Assert.Equal("USA", request.JurisdictionCountry);
        Assert.Equal("REG-12345", request.RegistrationNumber);

        Assert.Equal(request.SubjectAlternativeNames.Length, 3);
        Assert.True(request.SubjectAlternativeNames.Contains("example.com"));
        Assert.True(request.SubjectAlternativeNames.Contains("www.example.com"));
        Assert.True(request.SubjectAlternativeNames.Contains("192.168.1.1"));
    }

    static void ConfigureLogging()
    {
        var config = new NLog.Config.LoggingConfiguration();

        // Targets where to log to: File and Console
        var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
        logconsole.Layout = @"${date:format=HH\:mm\:ss} ${logger} [${level}] - ${message}";

        // Rules for mapping loggers to targets            
        config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logconsole);

        // Apply config           
        NLog.LogManager.Configuration = config;

        LogHandler.Factory = LoggerFactory.Create(builder =>
                {
                builder.AddNLog();
                });
    }

    static string GenerateCSR(string subject)
    {
        using RSA rsa = RSA.Create(2048);
        X500DistinguishedName subjectName = new X500DistinguishedName(subject);
        CertificateRequest csr = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return csr.CreateSigningRequestPem();
    }
}

