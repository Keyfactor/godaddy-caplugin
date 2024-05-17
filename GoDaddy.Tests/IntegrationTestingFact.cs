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

namespace GoDaddy.Tests;

public sealed class IntegrationTestingFact : FactAttribute
{
    public string BaseApiUrl { get; private set; }
    public string ApiKey { get; private set; }
    public string ApiSecret { get; private set; }
    public string ShopperId { get; private set; }

    public IntegrationTestingFact()
    {
        BaseApiUrl = Environment.GetEnvironmentVariable("GODADDY_API_URL") ?? string.Empty;
        ApiKey = Environment.GetEnvironmentVariable("GODADDY_API_KEY") ?? string.Empty;
        ApiSecret = Environment.GetEnvironmentVariable("GODADDY_API_SECRET") ?? string.Empty;
        ShopperId = Environment.GetEnvironmentVariable("GODADDY_SHOPPER_ID") ?? string.Empty;

        if (string.IsNullOrEmpty(BaseApiUrl) || string.IsNullOrEmpty(ApiKey) || string.IsNullOrEmpty(ApiSecret) || string.IsNullOrEmpty(ShopperId))
        {
            Skip = "Integration testing environment variables are not set - Skipping test";
        }
    }
}

