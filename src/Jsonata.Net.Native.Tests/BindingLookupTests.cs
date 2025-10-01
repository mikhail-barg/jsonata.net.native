using Jsonata.Net.Native;
using Jsonata.Net.Native.Eval;
using Jsonata.Net.Native.Json;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public sealed class BindingLookupTests
    {
        [Test]
        public void CanLookupBoundVariable()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction("getVar", GetVariable);
            env.BindValue("myVar", new JValue("test value"));

            JsonataQuery query = new JsonataQuery("$getVar()");
            JToken result = query.Eval(JValue.CreateNull(), env);

            Assert.AreEqual("test value", (string)result);
        }

        [Test]
        public void ReturnsUndefinedWhenVariableNotBound()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction("getVar", GetVariable);

            JsonataQuery query = new JsonataQuery("$getVar()");
            JToken result = query.Eval(JValue.CreateNull(), env);

            Assert.AreEqual(JTokenType.Undefined, result.Type);
        }

        [Test]
        public void CanCombineWithRegularParameters()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction("concat", ConcatWithVariable);
            env.BindValue("suffix", new JValue(" world"));

            JsonataQuery query = new JsonataQuery("$concat('hello')");
            JToken result = query.Eval(JValue.CreateNull(), env);

            Assert.AreEqual("hello world", (string)result);
        }

        [Test]
        public void CanAccessMultipleVariables()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction("fullName", BuildFullName);
            env.BindValue("firstName", new JValue("John"));
            env.BindValue("lastName", new JValue("Doe"));

            JsonataQuery query = new JsonataQuery("$fullName()");
            JToken result = query.Eval(JValue.CreateNull(), env);

            Assert.AreEqual("John Doe", (string)result);
        }

        [Test]
        public void ErrorMessageExcludesInjectedParameterFromCount()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction("test", FunctionRequiringOneArg);

            JsonataQuery query = new JsonataQuery("$test()");
            JsonataException? ex = Assert.Throws<JsonataException>(() =>
                query.Eval(JValue.CreateNull(), env));

            Assert.AreEqual("T0410", ex!.Code);
            Assert.That(ex!.Message, Does.Contain("requires 1"));
        }

        public static JToken GetVariable(IBindingLookup lookup)
        {
            return lookup.Lookup("myVar");
        }

        public static JToken ConcatWithVariable(string prefix, IBindingLookup lookup)
        {
            JToken suffix = lookup.Lookup("suffix");
            if (suffix.Type == JTokenType.String)
            {
                return new JValue(prefix + (string)suffix);
            }
            return new JValue(prefix);
        }

        public static JToken BuildFullName(IBindingLookup lookup)
        {
            JToken firstName = lookup.Lookup("firstName");
            JToken lastName = lookup.Lookup("lastName");

            if (firstName.Type == JTokenType.String && lastName.Type == JTokenType.String)
            {
                return new JValue($"{(string)firstName} {(string)lastName}");
            }
            return EvalProcessor.UNDEFINED;
        }

        public static JToken FunctionRequiringOneArg(string required, IBindingLookup lookup)
        {
            return new JValue("result");
        }
    }
}
