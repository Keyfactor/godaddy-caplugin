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
using Keyfactor.PKI.Enums.EJBCA;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using static GoDaddy.GoDaddyCAPluginConfig;

namespace GoDaddy.Tests;

public class GoDaddyCAPluginTests
{
    ILogger _logger { get; set;}

    public GoDaddyCAPluginTests()
    {
        ConfigureLogging();

        _logger = LogHandler.GetClassLogger<GoDaddyCAPluginTests>();
    }

    [IntegrationTestingFact]
    public void GoDaddyCAPlugin_Integration_SynchronizeCAFull_ReturnSuccess()
    {
        // Arrange
        IntegrationTestingFact env = new();
        GoDaddyCAPluginConfig.Config config = new GoDaddyCAPluginConfig.Config()
        {
            ApiKey = env.ApiKey,
            ApiSecret = env.ApiSecret,
            BaseUrl = env.BaseApiUrl,
            ShopperId = env.ShopperId
        };

        IAnyCAPluginConfigProvider configProvider = new FakeCaConfigProvider(config);
        ICertificateDataReader certificateDataReader = new FakeCertificateDataReader();

        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin();
        plugin.Initialize(configProvider, certificateDataReader);

        BlockingCollection<AnyCAPluginCertificate> certificates = new BlockingCollection<AnyCAPluginCertificate>();

        // Act
        plugin.Synchronize(certificates, DateTime.Now, true, CancellationToken.None).Wait();

        // Assert
        Assert.True(certificates.Count > 0);
    }

    [Fact]
    public void GoDaddyCAPlugin_SynchronizeCAFull_ReturnSuccess()
    {
        // Arrange
        var testRevokeDate = DateTime.Now;
        IGoDaddyClient client = new FakeGoDaddyClient()
        {
            CertificatesIssuedByFakeGoDaddy = new Dictionary<string, AnyCAPluginCertificate>
            {
                { "test-cert-1", new AnyCAPluginCertificate
                    {
                        CARequestID = "fake-ca-request-id",
                        Certificate = "fake-certificate",
                        Status = 123,
                        ProductID = "fake-product-id",
                        RevocationDate = testRevokeDate,
                    }
                }
            }
        };

        BlockingCollection<AnyCAPluginCertificate> certificates = new BlockingCollection<AnyCAPluginCertificate>();
        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = client
        };

        // Act
        plugin.Synchronize(certificates, DateTime.Now, true, CancellationToken.None).Wait();

        // Assert
        Assert.Equal("fake-ca-request-id", certificates.First().CARequestID);
        Assert.Equal("fake-certificate", certificates.First().Certificate);
        Assert.Equal(123, certificates.First().Status);
        Assert.Equal("fake-product-id", certificates.First().ProductID);
        Assert.Equal(testRevokeDate, certificates.First().RevocationDate);
    }

    [Fact]
    public void GoDaddyCAPlugin_ValidateProductInfo_DV_InvalidParameters_ReturnFailure()
    {
        // Arrange
        FakeGoDaddyClient fakeClient = new FakeGoDaddyClient();
        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = fakeClient
        };

        EnrollmentProductInfo productInfo = new EnrollmentProductInfo
        {
            ProductID = "DV_SSL",
            ProductParameters = new Dictionary<string, string>
            {
                { EnrollmentConfigConstants.JobTitle, "Software Engineer" },
                { EnrollmentConfigConstants.FirstName, "John" },
                { EnrollmentConfigConstants.Email, "john.doe@example.com" },
                { EnrollmentConfigConstants.Phone, "123-456-7890" },
                { EnrollmentConfigConstants.SlotSize, "1024" },
                { EnrollmentConfigConstants.CertificateValidityInYears, "2" },
            }
        };

        bool success = true;
        // Act
        try
        {
            plugin.ValidateProductInfo(productInfo, new Dictionary<string, object>()).Wait();
        }
        catch
        {
            success = false;
        }

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void GoDaddyCAPlugin_ValidateProductInfo_OV_InvalidParameters_ReturnFailure()
    {
        // Arrange
        FakeGoDaddyClient fakeClient = new FakeGoDaddyClient();
        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = fakeClient
        };

        EnrollmentProductInfo productInfo = new EnrollmentProductInfo
        {
            ProductID = "OV_SSL",
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
            }
        };

        bool success = true;
        // Act
        try
        {
            plugin.ValidateProductInfo(productInfo, new Dictionary<string, object>()).Wait();
        }
        catch
        {
            success = false;
        }

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void GoDaddyCAPlugin_ValidateProductInfo_EV_InvalidParameters_ReturnFailure()
    {
        // Arrange
        FakeGoDaddyClient fakeClient = new FakeGoDaddyClient();
        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = fakeClient
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
            }
        };
        bool success = true;

        // Act
        try
        {
            plugin.ValidateProductInfo(productInfo, new Dictionary<string, object>()).Wait();
        }
        catch
        {
            success = false;
        }

        // Assert
        Assert.False(success);
    }

    [Theory]
    [InlineData("DV_SSL")]
    [InlineData("DV_WILDCARD_SSL")]
    [InlineData("EV_SSL")]
    [InlineData("OV_CS")]
    [InlineData("OV_DS")]
    [InlineData("OV_SSL")]
    [InlineData("OV_WILDCARD_SSL")]
    [InlineData("UCC_DV_SSL")]
    [InlineData("UCC_EV_SSL")]
    [InlineData("UCC_OV_SSL")]
    public void GoDaddyCAPlugin_Enroll_ReturnSuccess(string productID)
    {
        // Arrange
        FakeGoDaddyClient fakeClient = new FakeGoDaddyClient();

        BlockingCollection<AnyCAPluginCertificate> certificates = new BlockingCollection<AnyCAPluginCertificate>();
        IAnyCAPluginConfigProvider configProvider = new FakeCaConfigProvider(new Config());
        ICertificateDataReader certificateDataReader = new FakeCertificateDataReader(fakeClient);

        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = fakeClient
        };
        plugin.Initialize(configProvider, certificateDataReader);
        
        // CSR
        string subject = "CN=Test Subject";
        string csrString = GenerateCSR(subject);

        Dictionary<string, string[]> sans = new();
        
        EnrollmentProductInfo productInfo = new EnrollmentProductInfo
        {
            ProductID = productID,
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

        // Unused
        RequestFormat format = new RequestFormat();

        EnrollmentType type = EnrollmentType.New;

        // Act
        EnrollmentResult result = plugin.Enroll(csrString, subject, sans, productInfo, format, type).Result;
        
        // Assert
        Assert.Equal(result.Status, (int)EndEntityStatus.GENERATED);
    }

    [Theory]
    [InlineData("DV_SSL")]
    [InlineData("DV_WILDCARD_SSL")]
    [InlineData("EV_SSL")]
    [InlineData("OV_CS")]
    [InlineData("OV_DS")]
    [InlineData("OV_SSL")]
    [InlineData("OV_WILDCARD_SSL")]
    [InlineData("UCC_DV_SSL")]
    [InlineData("UCC_EV_SSL")]
    [InlineData("UCC_OV_SSL")]
    public void GoDaddyCAPlugin_Renew_ReturnSuccess(string productID)
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
                        ProductID = productID,
                    }
                }
            }
        };

        // Renewal is only available 60 days prior to expiration of the previous certificate and 30 days after the 
        // expiration of the previous certificate. 

        fakeClient.EnrollmentNotBefore = enrollmentNotBefore;
        fakeClient.EnrollmentNotAfter = enrollmentNotAfter;

        BlockingCollection<AnyCAPluginCertificate> certificates = new BlockingCollection<AnyCAPluginCertificate>();

        IAnyCAPluginConfigProvider configProvider = new FakeCaConfigProvider(new Config());
        ICertificateDataReader certificateDataReader = new FakeCertificateDataReader(fakeClient);

        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = fakeClient
        };
        plugin.Initialize(configProvider, certificateDataReader);

        // CSR
        string subject = "CN=Test Subject";
        string csrString = GenerateCSR(subject);

        Dictionary<string, string[]> sans = new();
        
        EnrollmentProductInfo productInfo = new EnrollmentProductInfo
        {
            ProductID = productID,
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
                { EnrollmentConfigConstants.RegistrationNumber, "REG-12345" },
                { "PriorCertSN", fakeCertificate.SerialNumber }
            }
        };

        // Unused
        RequestFormat format = new RequestFormat();

        EnrollmentType type = EnrollmentType.Renew;

        // Act
        EnrollmentResult result = plugin.Enroll(csrString, subject, sans, productInfo, format, type).Result;
        
        // Assert
        Assert.Equal(result.Status, (int)EndEntityStatus.GENERATED);
        Assert.Equal(result.StatusMessage, $"Certificate with ID {fakeCaRequestId} has been renewed");
        Assert.Equal(result.CARequestID, fakeCaRequestId);
    }

    [Theory]
    [InlineData("DV_SSL")]
    [InlineData("DV_WILDCARD_SSL")]
    [InlineData("EV_SSL")]
    [InlineData("OV_CS")]
    [InlineData("OV_DS")]
    [InlineData("OV_SSL")]
    [InlineData("OV_WILDCARD_SSL")]
    [InlineData("UCC_DV_SSL")]
    [InlineData("UCC_EV_SSL")]
    [InlineData("UCC_OV_SSL")]
    public void GoDaddyCAPlugin_Reissue_ReturnSuccess(string productID)
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
                        ProductID = productID,
                    }
                }
            }
        };

        // Renewal is only available 60 days prior to expiration of the previous certificate and 30 days after the 
        // expiration of the previous certificate. 

        fakeClient.EnrollmentNotBefore = enrollmentNotBefore;
        fakeClient.EnrollmentNotAfter = enrollmentNotAfter;

        BlockingCollection<AnyCAPluginCertificate> certificates = new BlockingCollection<AnyCAPluginCertificate>();

        IAnyCAPluginConfigProvider configProvider = new FakeCaConfigProvider(new Config());
        ICertificateDataReader certificateDataReader = new FakeCertificateDataReader(fakeClient);

        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin()
        {
            Client = fakeClient
        };
        plugin.Initialize(configProvider, certificateDataReader);

        // CSR
        string subject = "CN=Test Subject";
        string csrString = GenerateCSR(subject);

        Dictionary<string, string[]> sans = new();
        
        EnrollmentProductInfo productInfo = new EnrollmentProductInfo
        {
            ProductID = productID,
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
                { EnrollmentConfigConstants.RegistrationNumber, "REG-12345" },
                { "PriorCertSN", fakeCertificate.SerialNumber }
            }
        };

        // Unused
        RequestFormat format = new RequestFormat();

        EnrollmentType type = EnrollmentType.Renew;

        // Act
        EnrollmentResult result = plugin.Enroll(csrString, subject, sans, productInfo, format, type).Result;
        
        // Assert
        Assert.Equal(result.Status, (int)EndEntityStatus.GENERATED);
        Assert.Equal(result.StatusMessage, $"Certificate with ID {fakeCaRequestId} has been reissued");
        Assert.Equal(result.CARequestID, fakeCaRequestId);
    }

    [IntegrationTestingFact]
    public void GoDaddyCAPlugin_Integration_Enroll_ReturnSuccess()
    {
        // Arrange
        IntegrationTestingFact env = new();
        GoDaddyCAPluginConfig.Config config = new GoDaddyCAPluginConfig.Config()
        {
            ApiKey = env.ApiKey,
            ApiSecret = env.ApiSecret,
            BaseUrl = env.BaseApiUrl,
            ShopperId = env.ShopperId
        };

        IAnyCAPluginConfigProvider configProvider = new FakeCaConfigProvider(config);
        ICertificateDataReader certificateDataReader = new FakeCertificateDataReader();

        GoDaddyCAPlugin plugin = new GoDaddyCAPlugin();
        plugin.Initialize(configProvider, certificateDataReader);

        BlockingCollection<AnyCAPluginCertificate> certificates = new BlockingCollection<AnyCAPluginCertificate>();

        string subject = "CN=example.com";
        string csrString = GenerateCSR(subject);
        Dictionary<string, string[]> sans = new();

        EnrollmentProductInfo productInfo = new EnrollmentProductInfo
        {
            ProductID = "DV_SSL",
            ProductParameters = new Dictionary<string, string>
            {
                { EnrollmentConfigConstants.JobTitle, "Software Engineer" },
                { EnrollmentConfigConstants.CertificateValidityInYears, "2" },
                { EnrollmentConfigConstants.LastName, "Doe" },
                { EnrollmentConfigConstants.FirstName, "John" },
                { EnrollmentConfigConstants.Email, "john.doe@example.com" },
                { EnrollmentConfigConstants.Phone, "123-456-7890" },
                { EnrollmentConfigConstants.SlotSize, "5" },
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
        EnrollmentType type = EnrollmentType.New;

        // Act
        EnrollmentResult result = plugin.Enroll(csrString, subject, sans, productInfo, requestFormat, type).Result;

        // Assert
        Assert.True(certificates.Count > 0);
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

