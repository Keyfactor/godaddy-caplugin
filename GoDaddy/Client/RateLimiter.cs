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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Keyfactor.Logging;
using Microsoft.Extensions.Logging;

namespace Keyfactor.Extensions.CAPlugin.GoDaddy.Client;

public class RateLimiter
{
    ILogger _logger = LogHandler.GetClassLogger<RateLimiter>();
    private static Mutex _rateLimitMutex = new Mutex();
    private readonly uint _requestLimitPerMinute;
    private readonly uint _requestLimitPerSecond;
    private readonly Queue<DateTime> _requestTimestamps;

    public RateLimiter(uint requestLimitPerMinute)
    {
        _requestLimitPerMinute = requestLimitPerMinute;
        _requestLimitPerSecond = requestLimitPerMinute / 60;
        _requestTimestamps = new Queue<DateTime>();
        _logger.LogDebug($"Set up rate limiter using Sliding Window algorithm with {_requestLimitPerMinute} requests per minute.");
    }

    public async Task WaitForWindowAsync()
    {
        while (true)
        {
            if (_rateLimitMutex.WaitOne(5000)) 
            {
                DateTime now = DateTime.UtcNow;

                // Remove requests that are older than one minute
                while (_requestTimestamps.Count > 0 && (now - _requestTimestamps.Peek()).TotalSeconds >= 60)
                {
                    _requestTimestamps.Dequeue();
                }

                // Check the number of requests in the last second
                int requestsInLastSecond = 0;
                foreach (var timestamp in _requestTimestamps)
                {
                    if ((now - timestamp).TotalSeconds < 1)
                    {
                        requestsInLastSecond++;
                    }
                }

                // Check if the request limit per second or per minute is exceeded
                if (_requestTimestamps.Count < _requestLimitPerMinute && requestsInLastSecond < _requestLimitPerSecond)
                {
                    // Add the current request timestamp
                    _requestTimestamps.Enqueue(now);
                    _rateLimitMutex.ReleaseMutex();
                    _logger.LogTrace("Request allowed.");
                    return;
                }
                _rateLimitMutex.ReleaseMutex();
            }
            else 
            {
                throw new Exception("Failed to acquire rate limit mutex");
            }

            await Task.Delay(50);
        }
    }
}
