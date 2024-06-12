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
using Keyfactor.Extensions.CAPlugin.GoDaddy;

namespace Keyfactor.Extensions.CAPlugin.GoDaddyTests;

public class FakeCaConfigProvider : IAnyCAPluginConfigProvider
{
    GoDaddyCAPluginConfig.Config Config { get; set; }

    public FakeCaConfigProvider(GoDaddyCAPluginConfig.Config config)
    {
        Config = config;
    }

    Dictionary<string, object> IAnyCAPluginConfigProvider.CAConnectionData
    {
        get 
        {
            return new Dictionary<string, object>
            {
                { GoDaddyCAPluginConfig.ConfigConstants.ApiKey, Config.ApiKey },
                { GoDaddyCAPluginConfig.ConfigConstants.ApiSecret, Config.ApiSecret },
                { GoDaddyCAPluginConfig.ConfigConstants.BaseUrl, Config.BaseUrl },
                { GoDaddyCAPluginConfig.ConfigConstants.ShopperId, Config.ShopperId },
                { GoDaddyCAPluginConfig.ConfigConstants.Enabled , Config.Enabled },
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

