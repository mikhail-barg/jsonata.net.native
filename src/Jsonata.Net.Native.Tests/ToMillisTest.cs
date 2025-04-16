using System;
using System.Globalization;
using Jsonata.Net.Native.Eval;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public class ToMillisTest
    {
        private static void Check(string dateStr, string? picture, long expectedMillis)
        {
            long result = BuiltinFunctions.toMillis(
                timestamp: dateStr,
                picture: picture ?? BuiltinFunctions.UTC_FORMAT
            );

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
            Check("2017-11-07T15:12:37.121Z", @"yyyy-MM-dd\THH:mm:ss.fffK", 1510067557121);
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

    }
}
