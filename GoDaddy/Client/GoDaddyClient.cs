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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Keyfactor.PKI.Enums.EJBCA;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

namespace GoDaddy.Client;

public class GoDaddyAuthenticator : AuthenticatorBase {
    private ILogger _logger = LogHandler.GetClassLogger<GoDaddyAuthenticator>();
    readonly string _baseUrl;
    readonly string _apiKey;
    readonly string _apiSecret;

    public GoDaddyAuthenticator(string apiToken, string apiSecret) : base("") {
        _apiKey = apiToken;
        _apiSecret = apiSecret;
    }

    protected override ValueTask<RestSharp.Parameter> GetAuthenticationParameter(string accessToken) {
        _logger.LogTrace($"Getting authentication parameter: {_apiKey}:{_apiSecret}");
        var parameter = new HeaderParameter(KnownHeaders.Authorization, $"sso-key {_apiKey}:{_apiSecret}");
        return new ValueTask<RestSharp.Parameter>(parameter);
    }
}

public class GoDaddyClient : IGoDaddyClient, IDisposable {
    private ILogger _logger;
    readonly RestClient _client;

    // Rate Limiter
    private static double _maxRequestsPerMinute = 60.0;
    private double _availableRequests = _maxRequestsPerMinute;
    private DateTime _lastUpdate = DateTime.Now;

    private static int _pageSize = 50;
    string _shopperId;
    string _customerId;

    public class Builder : IGoDaddyClientBuilder
    {
        private string _baseUrl { get; set; }
        private string _apiKey { get; set; }
        private string _apiSecret { get; set; }
        private string _shopperId { get; set; }
        
        public IGoDaddyClientBuilder WithApiKey(string apiToken)
        {
            _apiKey = apiToken;
            return this;
        }

        public IGoDaddyClientBuilder WithApiSecret(string apiSecret)
        { 
            _apiSecret = apiSecret;
            return this;
        }

        public IGoDaddyClientBuilder WithBaseUrl(string baseUrl)
        {
            _baseUrl = baseUrl;
            return this;
        }

        public IGoDaddyClientBuilder WithShopperId(string shopperId) {
            _shopperId = shopperId;
            return this;
        }

        public IGoDaddyClient Build()
        {
            IGoDaddyClient client = new GoDaddyClient(_baseUrl, _apiKey, _apiSecret, _shopperId);

            return client;
        }
    }

    public GoDaddyClient(string apiUrl, string apiKey, string apiSecret, string shopperId) {
        _logger = LogHandler.GetClassLogger<GoDaddyClient>();

        _logger.LogDebug($"Creating GoDaddyClient with API URL: {apiUrl}, API Key: {apiKey}, Shopper ID: {shopperId}");

        var options = new RestClientOptions(apiUrl){
            Authenticator = new GoDaddyAuthenticator(apiKey, apiSecret),
            ThrowOnAnyError = true
        };

        _client = new RestClient(options);

        this._shopperId = shopperId;
    }

    private string GetCustomerId() {
        if (string.IsNullOrEmpty(_shopperId)) {
            _logger.LogError("Shopper ID is required to get customer ID");
            throw new ArgumentNullException(nameof(_shopperId));
        }

        if (!string.IsNullOrEmpty(_customerId)) {
            _logger.LogTrace($"Returning cached customer ID: {_customerId}");
            return _customerId;
        }

        _logger.LogDebug($"Getting customer ID for shopper ID: {_shopperId}");

        string path = $"/v1/shoppers/{_shopperId}";
        IDictionary <string, string> query = new Dictionary<string, string> {
            { "includes", "customerId" }
        };

        ShopperDetailsRestResponse shopper = GetAsync<ShopperDetailsRestResponse>(path, query).Result;
        _logger.LogTrace($"Customer ID for shopper ID {_shopperId} is {shopper.customerId}");
        _customerId = shopper.customerId;
        return _customerId;
    }

    public async Task<string> DownloadCertificate(string certificateId) {
        _logger.LogDebug($"Downloading certificate with ID: {certificateId}");

        string path = $"/v1/certificates/{certificateId}/download";
        DownloadCertificateRestResponse certificate = await GetAsync<DownloadCertificateRestResponse>(path);

        return certificate.pems.certificate;
    }

    public async Task<int> GetAllIssuedCertificates(BlockingCollection<AnyCAPluginCertificate> certificatesBuffer, CancellationToken cancelToken)
    {
        _logger.LogDebug("Getting all issued certificates");

        string path = $"/v2/customers/{GetCustomerId()}/certificates";
        IDictionary<string, string> query = new Dictionary<string, string> {
            { "limit", _pageSize.ToString() },
        };

        int numberOfCertificates = 0;
        int currentPage = 1;
        bool allPagesDownloaded = false;

        do
        {
            if (cancelToken.IsCancellationRequested)
            {
                _logger.LogDebug("Cancellation requested - stopping certificate download");
                break;
            }

            _logger.LogTrace($"Getting page {currentPage} (page size {_pageSize.ToString()})");
            query["offset"] = currentPage.ToString();
            CustomerCertificatesRestResponse certificatesWithPagination = await GetAsync<CustomerCertificatesRestResponse>(path, query);

            foreach (CertificateDetail certificateDetail in certificatesWithPagination.certificates)
            {
                string endEntityCertificatePem = await DownloadCertificate(certificateDetail.certificateId);
                certificatesBuffer.Add(new AnyCAPluginCertificate()
                {
                    CARequestID = certificateDetail.certificateId,
                    Certificate = endEntityCertificatePem,
                    Status = GoDaddyCertificateStatusToCAStatus(certificateDetail.status),
                    ProductID = certificateDetail.type,
                    RevocationDate = certificateDetail.revokedAt
                });
            }

            currentPage++;
            allPagesDownloaded = certificatesWithPagination.pagination.previous == certificatesWithPagination.pagination.last;
        }
        while (!allPagesDownloaded);

        return numberOfCertificates;
    }

    public async Task<TResponse> GetAsync<TResponse>(string endpoint, IDictionary<string, string> query = null)
        where TResponse : class
    {
        _logger.LogTrace($"Setting up GET request to {endpoint}");
        var request = new RestRequest(endpoint, Method.Get);

        if (query == null)
        {
            query = new Dictionary<string, string>();
        }
        foreach (var param in query)
        {
            request.AddQueryParameter(param.Key, param.Value);
            _logger.LogTrace($"Adding query parameter: {param.Key}={param.Value}");
        }

        UpdateRateLimits();
        if (_availableRequests < 1)
        {
            _logger.LogTrace("Rate limit exceeded - waiting for more requests to be available");
            while (_availableRequests < 1)
            {
                await Task.Delay(100);
                UpdateRateLimits();
            }
        }
        _availableRequests--;

        _logger.LogTrace($"Sending GET request to {endpoint}");
        var response = await _client.ExecuteAsync(request);

        var expectedResponseCodeAttribute = (ApiResponseAttribute)Attribute.GetCustomAttribute(typeof(TResponse), typeof(ApiResponseAttribute));
        if (expectedResponseCodeAttribute != null && response.StatusCode == expectedResponseCodeAttribute.StatusCode)
        {
            _logger.LogTrace($"Received response with expected status code [{response.StatusCode}] - serializing response content to {typeof(TResponse).ToString()}");
            _logger.LogTrace($"Response content: {response.Content}");
            return JsonConvert.DeserializeObject<TResponse>(response.Content);
        }
        else
        {
            _logger.LogError($"Received response with unexpected status code [{response.StatusCode}]");
            if (response.Content != null)
            {
                var errorResponse = JsonConvert.DeserializeObject<Error>(response.Content);
                throw new ApiException("API Error occurred", errorResponse);
            }
            else
            {
                throw new Exception("Response was not successful and no content was returned.");
            }
        }
    }
    
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest body)
        where TRequest : class
        where TResponse : class, new()
    {
        var request = new RestRequest(endpoint, Method.Post).AddJsonBody(body);
        var response = await _client.ExecuteAsync(request);
        var expectedResponseCodeAttribute = (ApiResponseAttribute)Attribute.GetCustomAttribute(typeof(TResponse), typeof(ApiResponseAttribute));

        if (expectedResponseCodeAttribute != null && response.StatusCode == expectedResponseCodeAttribute.StatusCode)
        {
            // Handle successful response
            return JsonConvert.DeserializeObject<TResponse>(response.Content);
        }
        else
        {
            // Handle error response
            if (response.Content != null)
            {
                var errorResponse = JsonConvert.DeserializeObject<Error>(response.Content);
                throw new ApiException("API Error occurred", errorResponse);
            }
            else
            {
                throw new Exception("Response was not successful and no content was returned.");
            }
        }
    }

    private void UpdateRateLimits()
    {
        var currentTime = DateTime.Now;
        var secondsSinceUpdate = currentTime - _lastUpdate;
        _lastUpdate = currentTime;
        _availableRequests = Math.Min(_availableRequests + _maxRequestsPerMinute * secondsSinceUpdate.Seconds / 60.0, _maxRequestsPerMinute);
        _logger.LogTrace($"Updated rate limits - {_availableRequests} requests available");
    }

    record GoDaddySingleObject<T>(T Data);

    public void Dispose() {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static int GoDaddyCertificateStatusToCAStatus(string status)
    {
        switch (status)
        {
            case "CANCELED":
                return (int)EndEntityStatus.CANCELLED;
            case "DENIED":
                return (int)EndEntityStatus.FAILED;
            case "ISSUED":
                return (int)EndEntityStatus.GENERATED;
            case "PENDING_ISSUANCE":
                return (int)EndEntityStatus.INPROCESS;
            case "PENDING_REKEY":
                return (int)EndEntityStatus.INPROCESS;
            case "PENDING_REVOCATION":
                return (int)EndEntityStatus.INPROCESS;
            case "REVOKED":
                return (int)EndEntityStatus.REVOKED;
            default:
                return (int)EndEntityStatus.FAILED;
        }
    }
}
