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

using System.Security.Cryptography.X509Certificates;
using Keyfactor.AnyGateway.Extensions;

namespace Keyfactor.Extensions.CAPlugin.GoDaddyTests;

public class FakeCertificateDataReader : ICertificateDataReader
{
    FakeGoDaddyClient? _fakeClient;
    public FakeCertificateDataReader(FakeGoDaddyClient client)
    {
        _fakeClient = client;
    }

    public FakeCertificateDataReader()
    {
    }

    public Task<bool> DoesCertExistForRequestID(string caRequestID)
    {
        if (_fakeClient != null && _fakeClient.CertificatesIssuedByFakeGoDaddy != null && _fakeClient.CertificatesIssuedByFakeGoDaddy.ContainsKey(caRequestID))
        {
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public DateTime? GetExpirationDateByRequestId(string caRequestID)
    {
        if (_fakeClient != null && _fakeClient.CertificatesIssuedByFakeGoDaddy != null && _fakeClient.CertificatesIssuedByFakeGoDaddy.ContainsKey(caRequestID))
        {
            X509Certificate2 cert = X509Certificate2.CreateFromPem(_fakeClient.CertificatesIssuedByFakeGoDaddy[caRequestID].Certificate);
            return cert.NotAfter;
        }
        return null;
    }

    public Task<string> GetRequestIDBySerialNumber(string serialNumber)
    {
        if (_fakeClient == null || _fakeClient.CertificatesIssuedByFakeGoDaddy == null)
        {
            return Task.FromResult(string.Empty);
        }

        foreach (var cert in _fakeClient.CertificatesIssuedByFakeGoDaddy)
        {
            X509Certificate2 cert2 = X509Certificate2.CreateFromPem(cert.Value.Certificate);
            if (cert2.SerialNumber == serialNumber)
            {
                return Task.FromResult(cert.Key);
            }
        }

        return Task.FromResult(string.Empty);
    }

    public Task<int> GetStatusByRequestID(string caRequestID)
    {
        if (_fakeClient != null && _fakeClient.CertificatesIssuedByFakeGoDaddy != null && _fakeClient.CertificatesIssuedByFakeGoDaddy.ContainsKey(caRequestID))
        {
            return Task.FromResult(_fakeClient.CertificatesIssuedByFakeGoDaddy[caRequestID].Status);
        }
        return Task.FromResult(0);
    }
}
