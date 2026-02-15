using System;
using Jsonata.Net.Native.Eval;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public class ToMillisTest
    {
        private static void Check(string dateStr, string? picture, long expectedMillis)
        {
            long result = BuiltinFunctions.toMillis(timestamp: dateStr, picture: picture);

            Assert.AreEqual(expectedMillis, result);
        }

        [Test] 
        public void TestDocs_1()
        {
            //see https://docs.jsonata.org/date-time-functions
            Check("2017-11-07T15:12:37.121Z", null, 1510067557121);
        }

        [Test]
        public void TestDocs_1_ExplicitPicture()
        {
            //see https://docs.jsonata.org/date-time-functions
            Check("2017-11-07T15:12:37.121Z", BuiltinFunctions.UTC_FORMAT, 1510067557121);
        }

        [Test]
        public void TestDocs_1_withTime()
        {
            //see https://docs.jsonata.org/date-time-functions
            Check("2017-11-07T15:12:37.121+00:00", null, 1510067557121);
        }

        [Test]
        public void TestDocs_2()
        {
            //see https://docs.jsonata.org/date-time-functions
            Check("2017-11-07T15:07:54.972Z", null, 1510067274972);
        }

        [Test]
        public void TestNoMs_1_issue30()
        {
            Check("2025-08-06T12:00:00", null, 1754481600000);
        }

        [Test]
        public void TestNoTime_1_issue30()
        {
            Check("2025-03-29", null, 1743206400000);
        }

    }
}
