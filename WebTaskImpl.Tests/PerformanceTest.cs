using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NUnit.Framework;
using WebTasksImpl.Impl;
using WebTasksImpl.Models;

namespace WebTaskImpl.Tests
{
    [TestFixture]
    public class PerformanceTest
    {
        [Test]
        [Ignore]
        public void Run()
        {
            var runner = new WebTaskRunner(TimeSpan.FromMilliseconds(1));

            for (int i = 0; i < Int32.MaxValue; i++)
            {
                Thread.Sleep(10);

                runner.RunWebTask(() =>
                    {
                        var r = new PerfResult();

                        r.RandomValues = new List<long>();

                        var random = new Random();

                        for (int j = 0, max = random.Next(10000); j < max; j++)
                        {
                            r.RandomValues.Add(j);
                        }

                        return r;
                    }, new WebTaskConfiguration{ ResultExpiration = TimeSpan.FromMilliseconds(1)});

            }
        }

    }

    public class PerfResult : WebTaskResult
    {
        public List<Int64> RandomValues { get; set; }
    }
}
