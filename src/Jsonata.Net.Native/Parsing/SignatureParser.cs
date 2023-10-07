using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Parsing
{
    internal sealed class SignatureParser
    {
        private static readonly Dictionary<char, LambdaNode.ParamType> s_paramTypeChars;

        static SignatureParser()
        {
            s_paramTypeChars = LambdaNode.s_paramTypePriorityLetters.ToDictionary(t => t.Item2[0], t => t.Item1);
        }

        internal static LambdaNode.Signature Parse(string str)
        {
            SignatureParser parser = new SignatureParser(str);
            try
            {
                LambdaNode.Signature result = parser.TryParseSignature() ?? throw new Exception($"Signature string does not start with '<', should not happen");
                if (!parser.Finished)
                {
                    throw new Exception($"Signature string has some stray chars");
                };
                return result;
            }
            catch (Exception ex)
            {
                //TODO: proper code
                throw new JsonataException("XXX", $"Failed to parse lambda signature: {ex.Message}. Remainder string '{parser.GetRemainder()}', Whole string was: '{str}'");
            }
        }

        private readonly string m_str;
        private int m_pos = 0;

        private bool Finished => this.m_pos >= this.m_str.Length;
        private char Current => this.m_str[this.m_pos];

        private SignatureParser(string str)
        {
            this.m_str = str;
        }

        private void Advance()
        {
            ++this.m_pos;
        }

        private string GetRemainder()
        {
            return this.m_str.Substring(this.m_pos);
        }

        internal LambdaNode.Signature? TryParseSignature()
        {
            if (this.Finished || this.Current != '<')
            {
                return null;
            }
            this.Advance();
            List<LambdaNode.Param> args = new List<LambdaNode.Param>();
            while (!this.Finished && this.Current != ':' && this.Current != '>')
            {
                args.Add(this.ParseParam());
            };
            LambdaNode.Param? result = null;
            if (!this.Finished && this.Current == ':')
            {
                this.Advance();
                result = this.ParseParam();
            };
            if (this.Finished || this.Current != '>')
            {
                throw new Exception($"Signature string does not end with '>'");
            };
            this.Advance();
            return new LambdaNode.Signature(args, result);
        }

        private LambdaNode.Param ParseParam()
        {
            LambdaNode.ParamType paramType = this.ParseParamType();
            LambdaNode.Signature? signature = this.TryParseSignature();
            LambdaNode.ParamOpt opt = this.ParseParamOpt();
            return new LambdaNode.Param(paramType, opt, signature);
        }

        private LambdaNode.ParamOpt ParseParamOpt()
        {
            if (this.Finished)
            {
                return LambdaNode.ParamOpt.None;
            };
            LambdaNode.ParamOpt result = this.Current switch {
                '?' => LambdaNode.ParamOpt.Optional,
                '+' => LambdaNode.ParamOpt.Variadic,
                '-' => LambdaNode.ParamOpt.Contextable,
                _ => LambdaNode.ParamOpt.None
            };
            if (result != LambdaNode.ParamOpt.None)
            {
                this.Advance();
            };
            return result;
        }

        private LambdaNode.ParamType ParseParamType()
        {
            if (this.Finished)
            {
                throw new Exception("Failed to parse param type - string finished");
            }
            else if (this.Current == '(')
            {
                this.Advance();
                LambdaNode.ParamType result = LambdaNode.ParamType.None;
                while (!this.Finished && this.Current != ')')
                {
                    result |= ParseParamType();
                };
                if (this.Finished)
                {
                    throw new Exception("Param type group has no closing brace");
                };
                this.Advance(); //consume ')';
                if (result == LambdaNode.ParamType.None)
                {
                    throw new Exception("Empty param type group");
                }
                return result;
            }
            else if (s_paramTypeChars.TryGetValue(this.Current, out LambdaNode.ParamType pt))
            {
                this.Advance();
                return pt;
            }
            else
            {
                throw new Exception($"Unexpected param type char '{this.Current}'");
            };
        }
    }
}
