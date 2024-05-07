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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Keyfactor.AnyGateway.Extensions;

namespace GoDaddy.Tests;

public class TestCaConfigProvider : IAnyCAPluginConfigProvider
{
    GoDaddyCAPluginBuilder<FakeGoDaddyClient.Builder>.Config Config { get; set; }

    public TestCaConfigProvider(GoDaddyCAPluginBuilder<FakeGoDaddyClient.Builder>.Config config)
    {
        Config = config;
    }

    Dictionary<string, object> IAnyCAPluginConfigProvider.CAConnectionData
    {
        get 
        {
            return new Dictionary<string, object>
            {
                { GoDaddyCAPluginBuilder<FakeGoDaddyClient.Builder>.ConfigConstants.ApiKey, Config.ApiKey },
                    { GoDaddyCAPluginBuilder<FakeGoDaddyClient.Builder>.ConfigConstants.ApiSecret, Config.ApiSecret },
                    { GoDaddyCAPluginBuilder<FakeGoDaddyClient.Builder>.ConfigConstants.BaseUrl, Config.BaseUrl },
                    { GoDaddyCAPluginBuilder<FakeGoDaddyClient.Builder>.ConfigConstants.ShopperId, Config.ShopperId }
            };
        }
    }

    public static X509Certificate2 GetSelfSignedCert(string hostname)
    {
        RSA rsa = RSA.Create(2048);
        CertificateRequest req = new CertificateRequest($"CN={hostname}", rsa, HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

        SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new SubjectAlternativeNameBuilder();
        subjectAlternativeNameBuilder.AddDnsName(hostname);
        req.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());
        req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));        
        req.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("2.5.29.32.0"), new Oid("1.3.6.1.5.5.7.3.1") }, false));

        X509Certificate2 selfSignedCert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        Console.Write($"Created self-signed certificate for \"{hostname}\" with thumbprint {selfSignedCert.Thumbprint}\n");
        return selfSignedCert;
    }
}

public class TestCertificateDataReader : ICertificateDataReader
{
    public TestCertificateDataReader()
    {
    }

    public Task<bool> DoesCertExistForRequestID(string caRequestID)
    {
        throw new NotImplementedException();
    }

    public DateTime? GetExpirationDateByRequestId(string caRequestID)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetRequestIDBySerialNumber(string serialNumber)
    {
        throw new NotImplementedException();
    }

    public Task<int> GetStatusByRequestID(string caRequestID)
    {
        throw new NotImplementedException();
    }
}
