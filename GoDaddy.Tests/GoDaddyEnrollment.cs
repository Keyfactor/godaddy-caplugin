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
using Keyfactor.Extensions.CAPlugin.GoDaddy.Client;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog.Extensions.Logging;
using static Keyfactor.Extensions.CAPlugin.GoDaddy.GoDaddyCAPluginConfig;

namespace Keyfactor.Extensions.CAPlugin.GoDaddyTests;

public class EnrollmentAbstractionTests
{
    ILogger _logger { get; set;}

    public EnrollmentAbstractionTests()
    {
        ConfigureLogging();

        _logger = LogHandler.GetClassLogger<EnrollmentAbstractionTests>();
    }

    [Fact]
    public void EnrollmentStrategyFactory_Enrollment_ValidParameters_ReturnSuccess()
    {
        // Arrange
        string subject = "CN=Test Subject";
        string csrString = GenerateCSR(subject);

        EnrollmentRequest fakeRequest = new EnrollmentRequest
        {
            ProductType = CertificateEnrollmentType.DV_SSL,
            CSR = csrString,
            EnrollmentType = EnrollmentType.New,
            RootCAType = RootCAType.STARFIELD_SHA_2,
            SubjectAlternativeNames = new string[] { "example.com", "www.example.com" },
            CommonName = "example.com",
            IntelVPro = true,

            // Enrollment Config (specified by Workflow or Enrollment Parameters)
            CertificateValidityInYears = 2,
            SlotSize = "5",

            // DV
            LastName = "Doe",
            FirstName = "John",
            Email = "john.doe@example.com",
            Phone = "+1234567890",

            // OV
            OrganizationName = "Example Organization",
            OrganizationAddress = "123 Example St",
            OrganizationCity = "Example City",
            OrganizationState = "EX",
            OrganizationCountry = "Example Country",
            OrganizationPhone = "+0987654321",

            // EV
            JobTitle = "IT Manager",
            RegistrationAgent = "Example Agent",
            RegistrationNumber = "123456789",

            // AnyGateway REST config
            PriorCertSN = "123456789ABCDEF",
        };

        FakeGoDaddyClient fakeClient = new FakeGoDaddyClient();
        ICertificateDataReader fakeCertificateReader = new FakeCertificateDataReader(fakeClient);

        EnrollmentStrategyFactory factory = new EnrollmentStrategyFactory(fakeCertificateReader, fakeClient);

        // Act
        IEnrollmentStrategy strategy = factory.GetStrategy(fakeRequest).Result;

        // Assert
        Assert.Equal("Enrollment", strategy.StrategyName);
    }

    [Fact]
    public void EnrollmentStrategyFactory_Renewal_ValidParameters_ReturnSuccess()
    {
        // Arrange
        DateTime enrollmentNotBefore = DateTime.UtcNow.AddDays(-5);
        DateTime enrollmentNotAfter = DateTime.UtcNow.AddDays(20);
        X509Certificate2 fakeCertificate = FakeGoDaddyClient.GenerateSelfSignedCertificate(RSA.Create(2048), "CN=Test Cert", enrollmentNotBefore, enrollmentNotAfter);
        string fakeCaRequestId = Guid.NewGuid().ToString();
        
        FakeGoDaddyClient fakeClient = new FakeGoDaddyClient()
        {
            CertificatesIssuedByFakeGoDaddy = new Dictionary<string, AnyCAPluginCertificate>
            {
                { fakeCaRequestId, new AnyCAPluginCertificate
                    {
                        CARequestID = fakeCaRequestId,
                        Certificate = fakeCertificate.ExportCertificatePem(),
                        Status = 123,
                        ProductID = "DV_SSL",
                    }
                }
            }
        };
        ICertificateDataReader fakeCertificateReader = new FakeCertificateDataReader(fakeClient);

        string subject = "CN=Test Subject";
        string csrString = GenerateCSR(subject);

        EnrollmentRequest fakeRequest = new EnrollmentRequest
        {
            ProductType = CertificateEnrollmentType.DV_SSL,
            CSR = csrString,
            EnrollmentType = EnrollmentType.RenewOrReissue,
            RootCAType = RootCAType.STARFIELD_SHA_2,
            SubjectAlternativeNames = new string[] { "example.com", "www.example.com" },
            CommonName = "example.com",
            IntelVPro = true,

            // Enrollment Config (specified by Workflow or Enrollment Parameters)
            CertificateValidityInYears = 2,
            SlotSize = "5",

            // DV
            LastName = "Doe",
            FirstName = "John",
            Email = "john.doe@example.com",
            Phone = "+1234567890",

            // OV
            OrganizationName = "Example Organization",
            OrganizationAddress = "123 Example St",
            OrganizationCity = "Example City",
            OrganizationState = "EX",
            OrganizationCountry = "Example Country",
            OrganizationPhone = "+0987654321",

            // EV
            JobTitle = "IT Manager",
            RegistrationAgent = "Example Agent",
            RegistrationNumber = "123456789",

            // AnyGateway REST config
            PriorCertSN = fakeCertificate.SerialNumber
        };

        EnrollmentStrategyFactory factory = new EnrollmentStrategyFactory(fakeCertificateReader, fakeClient);

        // Act
        IEnrollmentStrategy strategy = factory.GetStrategy(fakeRequest).Result;

        // Assert
        Assert.Equal("Renewal", strategy.StrategyName);
    }

    [Fact]
    public void EnrollmentStrategyFactory_Reissue_ValidParameters_ReturnSuccess()
    {
        // Arrange
        DateTime enrollmentNotBefore = DateTime.UtcNow.AddDays(-100);
        DateTime enrollmentNotAfter = DateTime.UtcNow.AddDays(365);
        X509Certificate2 fakeCertificate = FakeGoDaddyClient.GenerateSelfSignedCertificate(RSA.Create(2048), "CN=Test Cert", enrollmentNotBefore, enrollmentNotAfter);
        string fakeCaRequestId = Guid.NewGuid().ToString();
        
        FakeGoDaddyClient fakeClient = new FakeGoDaddyClient()
        {
            CertificatesIssuedByFakeGoDaddy = new Dictionary<string, AnyCAPluginCertificate>
            {
                { fakeCaRequestId, new AnyCAPluginCertificate
                    {
                        CARequestID = fakeCaRequestId,
                        Certificate = fakeCertificate.ExportCertificatePem(),
                        Status = 123,
                        ProductID = "DV_SSL",
                    }
                }
            }
        };
        ICertificateDataReader fakeCertificateReader = new FakeCertificateDataReader(fakeClient);

        string subject = "CN=Test Subject";
        string csrString = GenerateCSR(subject);

        EnrollmentRequest fakeRequest = new EnrollmentRequest
        {
            ProductType = CertificateEnrollmentType.DV_SSL,
            CSR = csrString,
            EnrollmentType = EnrollmentType.RenewOrReissue,
            RootCAType = RootCAType.STARFIELD_SHA_2,
            SubjectAlternativeNames = new string[] { "example.com", "www.example.com" },
            CommonName = "example.com",
            IntelVPro = true,

            // Enrollment Config (specified by Workflow or Enrollment Parameters)
            CertificateValidityInYears = 2,
            SlotSize = "5",

            // DV
            LastName = "Doe",
            FirstName = "John",
            Email = "john.doe@example.com",
            Phone = "+1234567890",

            // OV
            OrganizationName = "Example Organization",
            OrganizationAddress = "123 Example St",
            OrganizationCity = "Example City",
            OrganizationState = "EX",
            OrganizationCountry = "Example Country",
            OrganizationPhone = "+0987654321",

            // EV
            JobTitle = "IT Manager",
            RegistrationAgent = "Example Agent",
            RegistrationNumber = "123456789",

            // AnyGateway REST config
            PriorCertSN = fakeCertificate.SerialNumber
        };

        EnrollmentStrategyFactory factory = new EnrollmentStrategyFactory(fakeCertificateReader, fakeClient);

        // Act
        IEnrollmentStrategy strategy = factory.GetStrategy(fakeRequest).Result;

        // Assert
        Assert.Equal("Reissue", strategy.StrategyName);
    }

    [Fact]
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
                { EnrollmentConfigConstants.SlotSize, "5" },
                { EnrollmentConfigConstants.CertificateValidityInYears, "2" },
                { EnrollmentConfigConstants.RootCAType, "GODADDY_SHA_2" },
                // DV
                { EnrollmentConfigConstants.LastName, "Doe" },
                { EnrollmentConfigConstants.FirstName, "John" },
                { EnrollmentConfigConstants.Email, "john.doe@example.com" },
                { EnrollmentConfigConstants.Phone, "123-456-7890" },
                // OV
                { EnrollmentConfigConstants.OrganizationName, "Example Corp" },
                { EnrollmentConfigConstants.OrganizationAddress, "1234 Elm Street" },
                { EnrollmentConfigConstants.OrganizationCity, "Example City" },
                { EnrollmentConfigConstants.OrganizationState, "EX" },
                { EnrollmentConfigConstants.OrganizationCountry, "USA" },
                { EnrollmentConfigConstants.OrganizationPhone, "987-654-3210" },
                // EV
                { EnrollmentConfigConstants.JobTitle, "Software Engineer" },
                { EnrollmentConfigConstants.RegistrationAgent, "Agent" },
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
        Assert.Equal("5", request.SlotSize);
        Assert.Equal(2, request.CertificateValidityInYears);
        // DV
        Assert.Equal("Doe", request.LastName);
        Assert.Equal("John", request.FirstName);
        Assert.Equal("john.doe@example.com", request.Email);
        Assert.Equal("123-456-7890", request.Phone);
        // OV
        Assert.Equal("Example Corp", request.OrganizationName);
        Assert.Equal("1234 Elm Street", request.OrganizationAddress);
        Assert.Equal("Example City", request.OrganizationCity);
        Assert.Equal("EX", request.OrganizationState);
        Assert.Equal("USA", request.OrganizationCountry);
        Assert.Equal("987-654-3210", request.OrganizationPhone);
        // EV
        Assert.Equal("Software Engineer", request.JobTitle);
        Assert.Equal("Agent", request.RegistrationAgent);
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

