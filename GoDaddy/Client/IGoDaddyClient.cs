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
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.Extensions;

namespace GoDaddy.Client;

public interface IGoDaddyClientBuilder
{
    public IGoDaddyClientBuilder WithBaseUrl(string baseUrl);
    public IGoDaddyClientBuilder WithApiKey(string apiToken);
    public IGoDaddyClientBuilder WithApiSecret(string apiSecret);
    public IGoDaddyClientBuilder WithShopperId(string shopperId);
    public IGoDaddyClient Build();
}

public interface IGoDaddyClient
{
    Task Ping();
    Task<CertificateDetailsRestResponse> GetCertificateDetails(string certificateId);
    Task<AnyCAPluginCertificate> DownloadCertificate(string certificateId);
    Task<string> DownloadCertificatePem(string certificateId);
    Task<int> DownloadAllIssuedCertificates(BlockingCollection<AnyCAPluginCertificate> certificatesBuffer, CancellationToken cancelToken);
    Task<EnrollmentResult> Enroll(CertificateOrderRestRequest request, CancellationToken cancelToken);
    Task<EnrollmentResult> Renew(string certificateId, RenewCertificateRestRequest request, CancellationToken cancelToken);
    Task<EnrollmentResult> Reissue(string certificateId, ReissueCertificateRestRequest request, CancellationToken cancelToken);
    Task RevokeCertificate(string certificateId, RevokeReason reason);
    Task CancelCertificateOrder(string certificateId);
}

