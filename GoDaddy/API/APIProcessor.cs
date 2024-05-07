// Copyright 2021 Keyfactor
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

using Keyfactor.PKI;
using GoDaddy.Models;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Runtime.ConstrainedExecution;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;
using RestSharp.Authenticators;

namespace GoDaddy.API;

internal class APIProcessor
{
    ILogger _logger = LogHandler.GetClassLogger<APIProcessor>();
    readonly RestClient _client;

    private string ApiUrl { get; set; }
    private string ApiKey { get; set; }
    private string ShopperId { get; set; }
    private int Timeout { get; set; }
    private int MaxNumberOfTimeouts { get; set; }

    private const string NO_CERTS_PURCHASED_MESSAGE = "Failed to create certificate order";

    private const string NO_CERTS_PURCHASED_REPL_MESSAGE = "Failed to create certificate order.  This error often occurs if there are no certificates purchased to fulfill this enrollment request.  " +
        "Please check your GoDaddy account to make sure you have the correct SSL certificate product purchased to cover this enrollment.";


    internal int TotalNumberOfTimeouts { get; set; } = 0;

    internal int TotalDurationOfDownloadApiCallsInMilliseconds { get; set; } = 0;

    public APIProcessor(string apiUrl, string apiKey, string shopperId, int timeout, int maxNumberOfTimeouts)
    {
        _logger.MethodEntry(LogLevel.Debug);

        ApiUrl = apiUrl;
        ApiKey = apiKey;
        ShopperId = shopperId;
        Timeout = timeout;
        MaxNumberOfTimeouts = maxNumberOfTimeouts;
        TotalNumberOfTimeouts = 0;

        _logger.MethodExit(LogLevel.Debug);
    }

    public string EnrollCSR(string csr, POSTCertificateRequest requestBody)
    {
        _logger.MethodEntry(LogLevel.Debug);

        string rtnMessage = string.Empty;

        string RESOURCE = "v1/certificates";
        RestRequest request = new RestRequest(RESOURCE, Method.Post);

        request.AddJsonBody(requestBody);

        _logger.MethodExit(LogLevel.Debug);

        _logger.LogTrace($"Json Request Body: {JsonConvert.SerializeObject(requestBody)}");

        return SubmitRequest(request);
    }

    public string RenewReissueCSR(string certificateId, string csr, POSTCertificateRenewalRequest requestBody, bool isRenew)
    {
        _logger.MethodEntry(LogLevel.Debug);

        string rtnMessage = string.Empty;
        string endpoint = isRenew ? "renew" : "reissue";

        string RESOURCE = $"v1/certificates/{certificateId}/{endpoint}";
        RestRequest request = new RestRequest(RESOURCE, Method.Post);

        request.AddJsonBody(requestBody);

        _logger.MethodExit(LogLevel.Debug);

        _logger.LogTrace($"Json Request Body: {JsonConvert.SerializeObject(requestBody)}");

        return SubmitRequest(request);
    }

    public string GetCertificates(string customerId, int pageNumber, int pageSize, int maxRetries)
    {
        _logger.MethodEntry(LogLevel.Debug);

        string rtnMessage = string.Empty;

        string RESOURCE = $"v2/customers/{customerId}/certificates?offset={pageNumber.ToString()}&limit={pageSize.ToString()}";
        RestRequest request = new RestRequest(RESOURCE, Method.Get);

        _logger.MethodExit(LogLevel.Debug);

        int retries = 0;
        while (true)
        {
            try
            {
                rtnMessage = SubmitRequest(request);
                break;
            }
            catch (GoDaddyTimeoutException ex)
            {
                retries++;
                if (retries > maxRetries)
                {
                    string msg = $"Maximum number of timeout retries of {maxRetries} exceeded for certificate page retrieval.";
                    _logger.LogError(msg);
                    throw new GoDaddyMaxTimeoutException(msg);
                }
                else
                    continue;
            }
        }

        return rtnMessage;
    }

    public string GetCertificate(string certificateId)
    {
        _logger.MethodEntry(LogLevel.Debug);

        string rtnMessage = string.Empty;

        string RESOURCE = $"v1/certificates/{certificateId}";
        RestRequest request = new RestRequest(RESOURCE, Method.Get);

        _logger.MethodExit(LogLevel.Debug);

        return SubmitRequest(request);
    }

    public string DownloadCertificate(string certificateId, int maxRetries)
    {
        _logger.MethodEntry(LogLevel.Debug);

        string cert = string.Empty;

        string RESOURCE = $"v1/certificates/{certificateId}/download";
        RestRequest request = new RestRequest(RESOURCE, Method.Get);

        _logger.MethodExit(LogLevel.Debug);

        int retries = 0;
        while (true)
        {
            try
            {
                DateTime before = DateTime.Now;
                cert = SubmitRequest(request);
                DateTime after = DateTime.Now;
                TotalDurationOfDownloadApiCallsInMilliseconds += after.Subtract(before).Milliseconds;

                break;
            }
            catch (GoDaddyTimeoutException ex)
            {
                retries++;
                if (retries > maxRetries)
                {
                    _logger.LogWarning($"Maximum number of timeout retries of {maxRetries} exceeded for certificate {certificateId} retrieval.  Certificate skipped.");
                    throw ex;
                }
                else
                    continue;
            }
        }

        return cert;
    }

    public void RevokeCertificate(string certificateId, POSTCertificateRevokeRequest.REASON reason)
    {
        _logger.MethodEntry(LogLevel.Debug);

        string rtnMessage = string.Empty;

        string RESOURCE = $"v1/certificates/{certificateId}/revoke";
        RestRequest request = new RestRequest(RESOURCE, Method.Post);

        POSTCertificateRevokeRequest body = new POSTCertificateRevokeRequest();
        body.reason = reason.ToString();

        request.AddJsonBody(body);

        _logger.LogTrace($"Json Request Body: {JsonConvert.SerializeObject(body)}");
        SubmitRequest(request);

        _logger.MethodExit(LogLevel.Debug);
    }

    public string GetCustomerId()
    {
        _logger.MethodEntry(LogLevel.Debug);

        string rtnMessage = string.Empty;

        string RESOURCE = $"v1/shoppers/{ShopperId}?includes=customerId";
        RestRequest request = new RestRequest(RESOURCE, Method.Get);

        _logger.MethodExit(LogLevel.Debug);

        return SubmitRequest(request);
    }

    public static int MapReturnStatus(CertificateStatusEnum status)
    {
        PKIConstants.Microsoft.RequestDisposition returnStatus = PKIConstants.Microsoft.RequestDisposition.UNKNOWN;

        switch (status)
        {
            case CertificateStatusEnum.DENIED:
                returnStatus = PKIConstants.Microsoft.RequestDisposition.DENIED;
                break;

            case CertificateStatusEnum.EXPIRED:
            case CertificateStatusEnum.CURRENT:
            case CertificateStatusEnum.ISSUED:
                returnStatus = PKIConstants.Microsoft.RequestDisposition.ISSUED;
                break;

            case CertificateStatusEnum.PENDING_ISSUANCE:
                returnStatus = PKIConstants.Microsoft.RequestDisposition.EXTERNAL_VALIDATION;
                break;

            case CertificateStatusEnum.REVOKED:
                returnStatus = PKIConstants.Microsoft.RequestDisposition.REVOKED;
                break;

            default:
                returnStatus = PKIConstants.Microsoft.RequestDisposition.FAILED;
                break;
        }

        return Convert.ToInt32(returnStatus);
    }

    public static POSTCertificateRevokeRequest.REASON MapRevokeReason(uint reason)
    {
        POSTCertificateRevokeRequest.REASON returnReason = POSTCertificateRevokeRequest.REASON.PRIVILEGE_WITHDRAWN;

        switch (reason)
        {
            case 1:
                returnReason = POSTCertificateRevokeRequest.REASON.KEY_COMPROMISE;
                break;

            case 3:
                returnReason = POSTCertificateRevokeRequest.REASON.AFFILIATION_CHANGED;
                break;

            case 4:
                returnReason = POSTCertificateRevokeRequest.REASON.SUPERSEDED;
                break;

            case 5:
                returnReason = POSTCertificateRevokeRequest.REASON.CESSATION_OF_OPERATION;
                break;
        }

        return returnReason;
    }

    #region Private Methods

    private string SubmitRequest(RestRequest request)
    {
        _logger.MethodEntry(LogLevel.Debug);
        _logger.LogTrace($"Request Resource: {request.Resource}");
        foreach (Parameter parameter in request.Parameters)
        {
            if (parameter.Name.ToLower() != "authorization")
                _logger.LogTrace($"{parameter.Name}: {parameter.Value.ToString()}");
        }
        _logger.LogTrace($"Request Method: {request.Method.ToString()}");

        RestResponse response = null;


        RestClient client = new RestClient(ApiUrl);

        try
        {
            response = client.Execute(request);
            _logger.LogTrace($"Http Status Code: {response.StatusCode}");
            _logger.LogTrace($"Response Status: {response.ResponseStatus}");

            if (response.ResponseStatus == ResponseStatus.TimedOut || response.StatusCode == 0)
            {
                string msg = "Request timed out. ";
                TotalNumberOfTimeouts++;

                if (TotalNumberOfTimeouts >= MaxNumberOfTimeouts)
                {
                    msg += $"Maximum timeouts of {MaxNumberOfTimeouts} exceeded.  ";
                    throw new GoDaddyMaxTimeoutException(msg);
                }
                else
                {
                    _logger.LogDebug(msg);
                    throw new GoDaddyTimeoutException(msg);
                }
            }
        }
        catch (GoDaddyTimeoutException ex) { throw ex; }
        catch (Exception ex)
        {
            string exceptionMessage = GoDaddyException.FlattenExceptionMessages(ex, $"Error processing {request.Resource}").Replace(NO_CERTS_PURCHASED_MESSAGE, NO_CERTS_PURCHASED_REPL_MESSAGE);
            _logger.LogError(exceptionMessage);
            throw ex;
        }

        if (response.StatusCode != System.Net.HttpStatusCode.OK &&
                response.StatusCode != System.Net.HttpStatusCode.Accepted &&
                response.StatusCode != System.Net.HttpStatusCode.Created &&
                response.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            string errorMessage;

            try
            {
                APIError error = JsonConvert.DeserializeObject<APIError>(response.Content);
                if (error == null)
                    errorMessage = "No error message returned.";
                else
                    errorMessage = $"{error.code}: {error.message}";
            }
            catch (JsonReaderException)
            {
                errorMessage = response.Content;
            }

            string exceptionMessage = $"Error processing {request.Resource}: {errorMessage.Replace(NO_CERTS_PURCHASED_MESSAGE, NO_CERTS_PURCHASED_REPL_MESSAGE)}";
            _logger.LogError(exceptionMessage);
            throw new GoDaddyException(exceptionMessage);
        }

        _logger.LogTrace($"API Result: {response.Content}");
        _logger.MethodExit(LogLevel.Debug);

        return response.Content;
    }

    #endregion Private Methods
}
