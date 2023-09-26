using System;
using Jsonata.Net.Native;
using Jsonata.Net.Native.Json;
using NUnit.Framework;

namespace Jsonata.Net.Native.Tests
{
    public sealed class FuncBindingTests
    {
        [Test]
        public void ViaMethodInfo()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction(typeof(FuncBindingTests).GetMethod(nameof(Mult3))!);
            JsonataQuery query = new JsonataQuery("( $Mult3(1, 2, 3); )");
            JToken result = query.Eval(JValue.CreateNull(), env);
            Assert.AreEqual(JTokenType.Integer, result.Type);
            Assert.AreEqual(6, (int)result);
        }

        [Test]
        public void ViaFuncStatic()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            Func<int, int, int, int> func = Mult3;
            env.BindFunction(nameof(Mult3), func);
            JsonataQuery query = new JsonataQuery("( $Mult3(1, 2, 3); )");
            JToken result = query.Eval(JValue.CreateNull(), env);
            Assert.AreEqual(JTokenType.Integer, result.Type);
            Assert.AreEqual(6, (int)result);
        }

        [Test]
        public void ViaInplaceStatic()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction(nameof(Mult3), Mult3);
            JsonataQuery query = new JsonataQuery("( $Mult3(1, 2, 3); )");
            JToken result = query.Eval(JValue.CreateNull(), env);
            Assert.AreEqual(JTokenType.Integer, result.Type);
            Assert.AreEqual(6, (int)result);
        }

        [Test]
        public void ViaFuncLambda()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            Func<int, int, int, int> func = (int a, int b, int c) => a * b * c;
            env.BindFunction(nameof(Mult3), func);
            JsonataQuery query = new JsonataQuery("( $Mult3(1, 2, 3); )");
            JToken result = query.Eval(JValue.CreateNull(), env);
            Assert.AreEqual(JTokenType.Integer, result.Type);
            Assert.AreEqual(6, (int)result);
        }

        [Test]
        public void ViaInplaceLambda()
        {
            EvaluationEnvironment env = new EvaluationEnvironment();
            env.BindFunction(nameof(Mult3), (int a, int b, int c) => a * b * c);
            JsonataQuery query = new JsonataQuery("( $Mult3(1, 2, 3); )");
            JToken result = query.Eval(JValue.CreateNull(), env);
            Assert.AreEqual(JTokenType.Integer, result.Type);
            Assert.AreEqual(6, (int)result);
        }

        public static int Mult3(int a, int b, int c)
        {
            return a * b * c;
        }
    }
}
