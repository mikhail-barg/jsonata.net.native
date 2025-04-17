using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Jsonata.Net.Native.Json;
using NUnit.Framework;
using ObjectParsingTestsData;

namespace Jsonata.Net.Native.SystemTextJson.Tests
{
    public sealed class UndefinedKindTests
    {
        [Test]
        public void CreateNodeWithUndefinedKind()
        {
            JsonNode node = JsonValue.Create(new JsonElement())!;
            Assert.That(node.GetValueKind(), Is.EqualTo(JsonValueKind.Undefined));
        }

        [Test]
        public void ConvertFromJTokenWithUndefined()
        {
            JToken jToken = JValue.CreateUndefined();
            JsonNode? node = jToken.ToSystemTextJsonNode();
            Assert.That(node, Is.Not.Null);
            Assert.That(node!.GetValueKind(), Is.EqualTo(JsonValueKind.Undefined));
        }

    }
}