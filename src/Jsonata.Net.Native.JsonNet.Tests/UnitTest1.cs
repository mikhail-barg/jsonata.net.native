using System;
using System.Globalization;
using NUnit.Framework;
using System.IO;
using Jsonata.Net.Native.Json;

namespace Jsonata.Net.Native.Tests
{
    public class UnitTest1
    {
        public void Test_Issue14_1()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = DateTime.Now });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }


        [Test]
        public void Test_Issue14_2()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = Guid.NewGuid() });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_3()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = TimeSpan.FromSeconds(5) });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_4()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = new Uri("http://abc.xyz") });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_5()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = DateTimeOffset.Now });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken);
            Assert.Pass();
        }

        [Test]
        public void Test_Issue14_6()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = new DateTime(2023, 09, 17, 22, 28, 00) });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken, CultureInfo.InvariantCulture, datetimeFormat: "yyyy~MM~dd HH:mm:ss");
            string value = (string)((Jsonata.Net.Native.Json.JObject)jToken).Properties["key"];
            Assert.That(value, Is.EqualTo("2023~09~17 22:28:00"));
        }

        [Test]
        public void Test_Issue14_7()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = new TimeSpan(10, 12, 13, 14, 156) });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken, CultureInfo.InvariantCulture, timespanFormat: @"ddd\-hh\-mm\-ss\-fff");
            string value = (string)((Jsonata.Net.Native.Json.JObject)jToken).Properties["key"];
            Assert.That(value, Is.EqualTo("010-12-13-14-156"));
        }

        [Test]
        public void Test_Issue14_8()
        {
            Newtonsoft.Json.Linq.JToken newtonsoftToken = Newtonsoft.Json.Linq.JToken.FromObject(new { key = Guid.Empty });
            Jsonata.Net.Native.Json.JToken jToken = Jsonata.Net.Native.JsonNet.JsonataExtensions.FromNewtonsoft(newtonsoftToken, CultureInfo.InvariantCulture, guidFormat: "N");
            string value = (string)((Jsonata.Net.Native.Json.JObject)jToken).Properties["key"];
            Assert.That(value, Is.EqualTo("00000000000000000000000000000000"));
        }

    }
}
