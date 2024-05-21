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
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.AnyGateway.Extensions;
using Keyfactor.Logging;
using Keyfactor.PKI.Enums.EJBCA;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Serializers.Json;

namespace GoDaddy.Client;

public class GoDaddyAuthenticator : AuthenticatorBase {
    readonly string _baseUrl;
    readonly string _apiKey;
    readonly string _apiSecret;

    public GoDaddyAuthenticator(string apiToken, string apiSecret) : base("") {
        _apiKey = apiToken;
        _apiSecret = apiSecret;
    }

    protected override ValueTask<RestSharp.Parameter> GetAuthenticationParameter(string accessToken) {
        var parameter = new HeaderParameter(KnownHeaders.Authorization, $"sso-key {_apiKey}:{_apiSecret}");
        return new ValueTask<RestSharp.Parameter>(parameter);
    }
}

public class GoDaddyClient : IGoDaddyClient, IDisposable {
    private ILogger _logger;
    readonly RestClient _client;

    bool _clientIsEnabled;

    RateLimiter rateLimiter = new RateLimiter(60);

    private static int _retriesPerRestOperation = 3;
    private static int _pageSize = 50;
    string _shopperId;
    string _customerId;

    public class Builder : IGoDaddyClientBuilder
    {
        private bool _enabled { get; set; }
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
        };

        JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        _client = new RestClient(
                options,
                configureSerialization: s => s.UseSystemTextJson(jsonSerializerOptions)
        );

        this._shopperId = shopperId;
        this._clientIsEnabled = true;
    }

    public Task Enable()
    {
        if (!_clientIsEnabled)
        {
            _logger.LogDebug("Enabling GoDaddy API client");
            _clientIsEnabled = true;
        }
        return Task.CompletedTask;
    }

    public Task Disable()
    {
        if (_clientIsEnabled)
        {
            _logger.LogDebug("Disabling GoDaddy API client");
            _clientIsEnabled = false;
        }
        return Task.CompletedTask;
    }

    public bool IsEnabled()
    {
        return _clientIsEnabled;
    }

    public async Task Ping()
    {
        EnsureClientIsEnabled();
        _logger.LogDebug("Validating GoDaddy API connection");

        string path = $"/v1/shoppers/{_shopperId}";
        IDictionary <string, string> query = new Dictionary<string, string> {
            { "includes", "customerId" }
        };

        try
        {
            ShopperDetailsRestResponse shopper = await GetAsync<ShopperDetailsRestResponse>(path, query);
            _logger.LogDebug($"Successfully connected to GoDaddy API with Shopper ID: {_shopperId}, Customer ID: {shopper.customerId}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed to connect to GoDaddy API: {e.Message}");
            throw;
        }
    }

    private string GetCustomerId() {
        EnsureClientIsEnabled();
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

    public async Task<AnyCAPluginCertificate> DownloadCertificate(string certificateId) {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Downloading certificate with ID: {certificateId}");

        string path = $"/v1/certificates/{certificateId}/download";
        DownloadCertificateRestResponse certificate = await GetAsync<DownloadCertificateRestResponse>(path);

        CertificateDetailsRestResponse details = await GetCertificateDetails(certificateId);

        return new AnyCAPluginCertificate
        {
            CARequestID = details.certificateId,
            Certificate = certificate.pems.certificate,
            Status = GoDaddyCertificateStatusToCAStatus(details.status),
            ProductID = details.productType,
            RevocationDate = details.revokedAt
        };
    }
    
    public async Task<string> DownloadCertificatePem(string certificateId) {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Downloading certificate with ID: {certificateId}");

        string path = $"/v1/certificates/{certificateId}/download";
        DownloadCertificateRestResponse certificate = await GetAsync<DownloadCertificateRestResponse>(path);

        return certificate.pems.certificate;
    }

    public async Task<CertificateDetailsRestResponse> GetCertificateDetails(string certificateId)
    {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Getting certificate details for certificate ID: {certificateId}");

        string path = $"/v1/certificates/{certificateId}";
        CertificateDetailsRestResponse certificateDetails = await GetAsync<CertificateDetailsRestResponse>(path);

        _logger.LogTrace($"Retrieved details for certificate with ID {certificateId} [status: {certificateDetails.status}, type: {certificateDetails.productType}]");

        return certificateDetails;
    }

    public async Task<int> DownloadAllIssuedCertificates(BlockingCollection<AnyCAPluginCertificate> certificatesBuffer, CancellationToken cancelToken)
    {
        EnsureClientIsEnabled();
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
                string debugMessage = "Downloading certificate ";
                if (!string.IsNullOrEmpty(certificateDetail.commonName)) 
                    debugMessage += $"with CN {certificateDetail.commonName} ";
                if (!string.IsNullOrEmpty(certificateDetail.validStartAt))
                    debugMessage += $"[issued at {certificateDetail.validStartAt}] ";
                if (!string.IsNullOrEmpty(certificateDetail.validEndAt))
                    debugMessage += $"[expires at {certificateDetail.validEndAt}] ";
                if (null != certificateDetail.revokedAt)
                    debugMessage += $"[revoked at {certificateDetail.revokedAt}]";
                _logger.LogDebug(debugMessage);

                string certificatePemString = await DownloadCertificatePem(certificateDetail.certificateId);
                certificatesBuffer.Add(new AnyCAPluginCertificate()
                {
                    CARequestID = certificateDetail.certificateId,
                    Certificate = certificatePemString,
                    Status = GoDaddyCertificateStatusToCAStatus(certificateDetail.status),
                    ProductID = certificateDetail.type,
                    RevocationDate = certificateDetail.revokedAt
                });
                numberOfCertificates++;
            }

            currentPage++;
            allPagesDownloaded = certificatesWithPagination.pagination.previous == certificatesWithPagination.pagination.last;
        }
        while (!allPagesDownloaded);

        certificatesBuffer.CompleteAdding();
        return numberOfCertificates;
    }

    public async Task<EnrollmentResult> Enroll(CertificateOrderRestRequest request, CancellationToken cancelToken)
    {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Enrolling CSR with common name {request.CommonName}");

        string path = $"/v1/certificates";
        CertificateOrderRestResponse certificateOrder = await PostAsync<CertificateOrderRestRequest, CertificateOrderRestResponse>(path, request);

        _logger.LogDebug($"Created Certificate order with ID {certificateOrder.certificateId} - Waiting for certificate to be issued");

        const int delay = 1000;
        CertificateDetailsRestResponse details;
        DateTime startTime = DateTime.Now;

        TaskCompletionSource<bool> callbackCompletionSource = new TaskCompletionSource<bool>();
        cancelToken.Register(async () =>
        {
            _logger.LogWarning($"Cancellation requested - cancelling certificate with ID {certificateOrder.certificateId}");
            await CancelCertificateOrder(certificateOrder.certificateId);
            callbackCompletionSource.SetResult(true);
        });

        do
        {
            if (cancelToken.IsCancellationRequested)
            {
                await callbackCompletionSource.Task;
                _logger.LogTrace("Cancellation callback task complete - throwing TaskCanceledException");
                throw new TaskCanceledException("Cancellation requested - certificate order cancelled");
            }
            details = await GetCertificateDetails(certificateOrder.certificateId);
            if ((int)EndEntityStatus.GENERATED == GoDaddyCertificateStatusToCAStatus(details.status))
            {
                _logger.LogDebug($"Certificate with ID {certificateOrder.certificateId} has been issued");
                break;
            }

            _logger.LogDebug($"Waiting for certificate with ID {certificateOrder.certificateId} to be issued ({DateTime.Now - startTime} elapsed)");

            await Task.Delay(delay);
        }
        while (true);

        // Sanity check
        if (details == null)
        {
            throw new Exception("Failed to get certificate details");
        }

        _logger.LogDebug($"Certificate with ID {certificateOrder.certificateId} has been issued - downloading certificate ({DateTime.Now - startTime} elapsed)");

        string certificatePemString = await DownloadCertificatePem(certificateOrder.certificateId);
        return new EnrollmentResult
        {
            CARequestID = certificateOrder.certificateId,
            Certificate = certificatePemString,
            Status = GoDaddyCertificateStatusToCAStatus(details.status),
            StatusMessage = $"Certificate with ID {certificateOrder.certificateId} has been issued",
        };
    }

    public async Task<EnrollmentResult> Reissue(string certificateId, ReissueCertificateRestRequest request, CancellationToken cancelToken)
    {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Reissuing certificate with ID {certificateId}");

        string path = $"/v1/certificates/{certificateId}/reissue";
        ReissueCertificateRestResponse certificateOrder = await PostAsync<ReissueCertificateRestRequest, ReissueCertificateRestResponse>(path, request);

        if (string.IsNullOrEmpty(certificateOrder.certificateId))
        {
            throw new Exception($"Certificate reissue failed - no certificate ID returned");
        }

        _logger.LogDebug($"Successfully submitted request to reissue certificate with ID {certificateId} - new certificate ID is {certificateOrder.certificateId} - Waiting for certificate to be issued");

        TaskCompletionSource<bool> callbackCompletionSource = new TaskCompletionSource<bool>();
        cancelToken.Register(async () =>
        {
            _logger.LogWarning($"Cancellation requested - cancelling certificate with ID {certificateOrder.certificateId}");
            await CancelCertificateOrder(certificateOrder.certificateId);
            callbackCompletionSource.SetResult(true);
        });

        const int delay = 1000;
        CertificateDetailsRestResponse details;
        do
        {
            if (cancelToken.IsCancellationRequested)
            {
                await callbackCompletionSource.Task;
                _logger.LogTrace("Cancellation callback task complete - throwing TaskCanceledException");
                throw new TaskCanceledException("Cancellation requested - certificate order cancelled");
            }

            details = await GetCertificateDetails(certificateOrder.certificateId);
            if ((int)EndEntityStatus.GENERATED == GoDaddyCertificateStatusToCAStatus(details.status))
            {
                _logger.LogDebug($"Certificate with ID {certificateId} has been reissued - new certificate ID is {certificateOrder.certificateId}");
                break;
            }

            await Task.Delay(delay);
        }
        while (true);

        // Sanity check
        if (details == null)
        {
            throw new Exception("Failed to get certificate details");
        }

        string certificatePemString = await DownloadCertificatePem(certificateOrder.certificateId);
        return new EnrollmentResult
        {
            CARequestID = details.certificateId,
            Certificate = certificatePemString,
            Status = GoDaddyCertificateStatusToCAStatus(details.status),
            StatusMessage = $"Certificate with ID {certificateId} has been reissued - new certificate ID is {certificateOrder.certificateId}"
        };
    }

    public async Task<EnrollmentResult> Renew(string certificateId, RenewCertificateRestRequest request, CancellationToken cancelToken)
    {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Renewing certificate with ID {certificateId}");

        string path = $"v1/certificates/{certificateId}/renew";
        RenewCertificateRestResponse certificateOrder = await PostAsync<RenewCertificateRestRequest, RenewCertificateRestResponse>(path, request);
        _logger.LogDebug($"Successfully submitted request to renew certificate with ID {certificateId} - new certificate ID is {certificateOrder.certificateId} - Waiting for certificate to be issued");

        TaskCompletionSource<bool> callbackCompletionSource = new TaskCompletionSource<bool>();
        cancelToken.Register(async () =>
        {
            _logger.LogWarning($"Cancellation requested - cancelling certificate with ID {certificateOrder.certificateId}");
            await CancelCertificateOrder(certificateOrder.certificateId);
            callbackCompletionSource.SetResult(true);
        });

        const int delay = 1000;
        CertificateDetailsRestResponse details;
        do
        {
            if (cancelToken.IsCancellationRequested)
            {
                await callbackCompletionSource.Task;
                _logger.LogTrace("Cancellation callback task complete - throwing TaskCanceledException");
                throw new TaskCanceledException("Cancellation requested - certificate order cancelled");
            }

            details = await GetCertificateDetails(certificateOrder.certificateId);
            if ((int)EndEntityStatus.GENERATED == GoDaddyCertificateStatusToCAStatus(details.status))
            {
                _logger.LogDebug($"Certificate with ID {certificateId} has been renewed - new certificate ID is {certificateOrder.certificateId}");
                break;
            }

            await Task.Delay(delay);
        }
        while (true);

        // Sanity check
        if (details == null)
        {
            throw new Exception("Failed to get certificate details");
        }

        string certificatePemString = await DownloadCertificatePem(certificateOrder.certificateId);
        return new EnrollmentResult
        {
            CARequestID = details.certificateId,
            Certificate = certificatePemString,
            Status = GoDaddyCertificateStatusToCAStatus(details.status),
            StatusMessage = $"Certificate with ID {certificateId} has been renewed - new certificate ID is {certificateOrder.certificateId}",
        };
    }

    public async Task RevokeCertificate(string certificateId, RevokeReason reason)
    {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Revoking certificate with ID {certificateId} [reason: {reason.ToString()}]");

        RevokeCertificateRestRequest request = new RevokeCertificateRestRequest(reason.ToString());

        string path = $"/v1/certificates/{certificateId}/revoke";
        await PostAsync<RevokeCertificateRestRequest, RevokeCertificateRestResponse>(path, request);
    }

    public async Task CancelCertificateOrder(string certificateId)
    {
        EnsureClientIsEnabled();
        _logger.LogDebug($"Cancelling certificate order with ID {certificateId}");

        CancelCertificateOrderRestRequest request = new CancelCertificateOrderRestRequest();

        string path = $"/v1/certificates/{certificateId}/cancel";
        await PostAsync<CancelCertificateOrderRestRequest, CancelCertificateOrderRestResponse>(path, request);
        _logger.LogDebug($"Successfully cancelled certificate order with ID {certificateId}");
    }

    public async Task<TResponse> GetAsync<TResponse>(string endpoint, IDictionary<string, string> query = null)
        where TResponse : class
    {
        EnsureClientIsEnabled();

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

        for (int i = 0; i < _retriesPerRestOperation; i++)
        {
            try
            {
                await rateLimiter.WaitForWindowAsync();

                _logger.LogTrace($"Sending GET request to {endpoint}");
                var response = await _client.ExecuteAsync(request);

                var expectedResponseCodeAttribute = (ApiResponseAttribute)Attribute.GetCustomAttribute(typeof(TResponse), typeof(ApiResponseAttribute));
                if (expectedResponseCodeAttribute != null && response.StatusCode == expectedResponseCodeAttribute.StatusCode && response.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogTrace($"Received response with expected status code [{response.StatusCode}] - skipping serialization to {typeof(TResponse).ToString()}");
                    return Activator.CreateInstance<TResponse>();
                }
                else if (expectedResponseCodeAttribute != null && response.StatusCode == expectedResponseCodeAttribute.StatusCode)
                {
                    _logger.LogTrace($"Received response with expected status code [{response.StatusCode}] - serializing response content to {typeof(TResponse).ToString()}");
                    return JsonSerializer.Deserialize<TResponse>(response.Content);
                }

                _logger.LogError($"Received response with unexpected status code [{response.StatusCode}]");
                if (response.Content == null)
                {
                    throw new Exception("Response was not successful and no content was returned.");
                }

                if (!string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogTrace($"Error content: {response.Content}");
                }

                Error errorResponse;
                try
                {
                    _logger.LogTrace("Serializing response content to error object");
                    
                    JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };
                    errorResponse = JsonSerializer.Deserialize<Error>(response.Content, jsonSerializerOptions);
                }
                catch (JsonException)
                {
                    throw new Exception($"Failed to GET {endpoint}: {response.Content} (failed to serialize to error)");
                }
                if (errorResponse.IsRateLimitError())
                {
                    _logger.LogWarning($"Rate limit exceeded - waiting for more requests to be available");
                    await rateLimiter.WaitForWindowAsync();
                    continue;
                }

                string message = $"Failed to GET {endpoint}: {errorResponse.ToString()}";
                _logger.LogError(message);
                throw new Exception(message);
            }
            catch (Exception e)
            {
                _logger.LogError($"Retry handler - {e.Message}");
                if (i == _retriesPerRestOperation - 1)
                {
                    _logger.LogError($"Failed to GET {endpoint} after {_retriesPerRestOperation} retries");
                    throw;
                }
                _logger.LogWarning($"Retrying GET request to {endpoint} [{i + 1}/{_retriesPerRestOperation}]");
            }
        }

        throw new Exception("Failed to GET request after all retries");
    }
    
    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest body, IDictionary<string, string> query = null)
        where TRequest : class
        where TResponse : class
    {
        EnsureClientIsEnabled();

        _logger.LogTrace($"Setting up POST request to {endpoint}");
        var request = new RestRequest(endpoint, Method.Post);
        request.AddJsonBody<TRequest>(body);

        if (query == null)
        {
            query = new Dictionary<string, string>();
        }
        foreach (var param in query)
        {
            request.AddQueryParameter(param.Key, param.Value);
            _logger.LogTrace($"Adding query parameter: {param.Key}={param.Value}");
        }

        for (int i = 0; i < _retriesPerRestOperation; i++)
        {
            try
            {
                await rateLimiter.WaitForWindowAsync();

                _logger.LogTrace($"Sending POST request to {endpoint}");
                RestResponse response;
                try
                {
                    response = await _client.ExecuteAsync(request);
                }
                catch (HttpRequestException e)
                {
                    _logger.LogError($"Failed to POST {endpoint}: {e.Message}");
                    throw;
                }

                var expectedResponseCodeAttribute = (ApiResponseAttribute)Attribute.GetCustomAttribute(typeof(TResponse), typeof(ApiResponseAttribute));
                if (expectedResponseCodeAttribute != null && response.StatusCode == expectedResponseCodeAttribute.StatusCode && response.StatusCode == HttpStatusCode.NoContent)
                {
                    _logger.LogTrace($"Received response with expected status code [{response.StatusCode}] - skipping serialization to {typeof(TResponse).ToString()}");
                    return Activator.CreateInstance<TResponse>();
                }
                else if (expectedResponseCodeAttribute != null && response.StatusCode == expectedResponseCodeAttribute.StatusCode)
                {
                    _logger.LogTrace($"Received response with expected status code [{response.StatusCode}] - serializing response content to {typeof(TResponse).ToString()}");
                    return JsonSerializer.Deserialize<TResponse>(response.Content);
                }

                _logger.LogError($"Received response with unexpected status code [{response.StatusCode}]");
                if (response.Content == null)
                {
                    _logger.LogError($"Response was not successful and no content was returned.");
                    throw new Exception("Response was not successful and no content was returned.");
                }

                if (!string.IsNullOrEmpty(response.Content))
                {
                    _logger.LogTrace($"Error content: {response.Content}");
                }

                Error errorResponse;
                try
                {
                    _logger.LogTrace("Serializing response content to error object");
                    
                    JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    };
                    errorResponse = JsonSerializer.Deserialize<Error>(response.Content, jsonSerializerOptions);
                }
                catch (JsonException)
                {
                    _logger.LogError($"Failed to POST {endpoint}: {response.Content} (failed to serialize to error)");
                    throw new Exception($"Failed to GET {endpoint}: {response.Content} (failed to serialize to error)");
                }

                string message = $"Failed to POST {endpoint}: {errorResponse.ToString()}";
                _logger.LogError(message);
                throw new Exception(message);
            }
            catch (Exception)
            {
                if (i == _retriesPerRestOperation - 1)
                {
                    _logger.LogError($"Failed to POST {endpoint} after {_retriesPerRestOperation} retries");
                    throw;
                }
                _logger.LogWarning($"Retrying POST request to {endpoint} [{i + 1}/{_retriesPerRestOperation}]");
            }
        }

        throw new Exception("Failed to POST request after all retries");
    }

    private void EnsureClientIsEnabled()
    {
        if (!_clientIsEnabled)
        {
            _logger.LogWarning("GoDaddy API client is disabled - throwing");
            throw new Exception("GoDaddy API client is disabled");
        }
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

