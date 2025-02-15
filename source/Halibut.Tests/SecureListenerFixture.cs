﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using FluentAssertions;
using Halibut.Diagnostics;
using Halibut.Transport;
using NUnit.Framework;

namespace Halibut.Tests
{
    public class SecureListenerFixture
    {
        PerformanceCounter GetCounterForCurrentProcess(string categoryName, string counterName)
        {
            var pid = Process.GetCurrentProcess().Id;
            
            var instanceName = new PerformanceCounterCategory("Process")
                .GetInstanceNames()
                .FirstOrDefault(instance =>
                {
                    using (var counter = new PerformanceCounter("Process", "ID Process", instance, true))
                    {
                        return pid == counter.RawValue;
                    }
                });

            if (instanceName == null)
            {
                throw new Exception("Could not find instance name for process.");
            }
            
            return new PerformanceCounter(categoryName, counterName, instanceName, true);
        }
        
        [Test]
        [WindowsTest]
        public void SecureListenerDoesNotCreateHundredsOfIoEventsPerSecondOnWindows()
        {
            const int secondsToSample = 5;

            using (var opsPerSec = GetCounterForCurrentProcess("Process", "IO Other Operations/sec"))
            {
                var client = new SecureListener(
                    new IPEndPoint(new IPAddress(new byte[]{ 127, 0, 0, 1 }), 1093), 
                    Certificates.TentacleListening,
                    null,
                    null,
                    thumbprint => true,
                    new LogFactory(), 
                    () => ""
                );

                var idleAverage = CollectCounterValues(opsPerSec)
                    .Take(secondsToSample)
                    .Average();

                float listeningAverage;
            
                using (client)
                {
                    client.Start();
                
                    listeningAverage = CollectCounterValues(opsPerSec)
                        .Take(secondsToSample)
                        .Average();
                }

                var idleAverageWithErrorMargin = idleAverage * 250f;
            
                TestContext.Out.WriteLine($"idle average:      {idleAverage} ops/second");
                TestContext.Out.WriteLine($"listening average: {listeningAverage} ops/second");
                TestContext.Out.WriteLine($"expectation:     < {idleAverageWithErrorMargin} ops/second");

                listeningAverage.Should().BeLessThan(idleAverageWithErrorMargin);
            }
        }
        
        IEnumerable<float> CollectCounterValues(PerformanceCounter counter)
        {
            var sleepTime = TimeSpan.FromSeconds(1);
            
            while (true)
            {
                Thread.Sleep(sleepTime);
                yield return counter.NextValue();
            }
        }
    }
}