using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Parsing
{
	[Flags]
    internal enum ParamType
    {
		ParamTypeNumber = 1 << 0,
		ParamTypeString = 1 << 1,
		ParamTypeBool	= 1 << 2,
		ParamTypeNull	= 1 << 3,
		ParamTypeArray	= 1 << 4,
		ParamTypeObject = 1 << 5,
		ParamTypeFunc	= 1 << 6,
		ParamTypeJSON	= 1 << 7,
		ParamTypeAny	= 1 << 8,
	}

	// A ParamOpt represents the options on a parameter in a lambda
	// function signature.
	internal enum ParamOpt
	{
		None,

		// ParamOptional denotes an optional parameter.
		ParamOptional,

		// ParamVariadic denotes a variadic parameter.
		ParamVariadic,

		// ParamContextable denotes a parameter that can be
		// replaced by the evaluation context if no value is
		// provided by the caller.
		ParamContextable
	}

	// A Param represents a parameter in a lambda function signature.
	internal sealed class Param 
	{
		internal readonly ParamType paramType;
		internal ParamOpt option = ParamOpt.None;
		internal List<Param>? subParams = null;

		internal Param(ParamType paramType)
        {
			this.paramType = paramType;
        }

        public override string ToString()
        {
			StringBuilder builder = new StringBuilder();
			builder.Append(this.paramType.ParamTypeToString());
			if (this.subParams != null)
            {
				builder.Append('<');
				for (int i = 0; i < this.subParams.Count; ++i)
				{
					builder.Append(this.subParams[i].ToString());
					//TODO: add comma?
				}
				builder.Append('>');
			}
			builder.Append(this.option.ParamOptToString());
			return builder.ToString();
        }
    }

	internal static class ParamExtensions
    {
		internal static bool TryParseParamType(char r, [NotNullWhen(true)] out ParamType? type)
        {
			switch (r)
			{
			case 'n':
				type = ParamType.ParamTypeNumber;
				return true;
			case 's':
				type = ParamType.ParamTypeString;
				return true;
			case 'b':
				type = ParamType.ParamTypeBool;
				return true;
			case 'l':
				type = ParamType.ParamTypeNull;
				return true;
			case 'a':
				type = ParamType.ParamTypeArray;
				return true;
			case 'o':
				type = ParamType.ParamTypeObject;
				return true;
			case 'f':
				type = ParamType.ParamTypeFunc;
				return true;
			case 'j':
				type = ParamType.ParamTypeJSON;
				return true;
			case 'x':
				type = ParamType.ParamTypeAny;
				return true;
			default:
				type = null;
				return false;
			}
		}

		internal static string ParamTypeToString(this ParamType type)
        {
			string s = "";
			if ((type & ParamType.ParamTypeNumber) != 0) 
			{
				s += 'n';
			}
			if ((type & ParamType.ParamTypeString) != 0)
			{
				s += "s";
			}
			if ((type & ParamType.ParamTypeBool) != 0)
			{
				s += "b";
			}
			if ((type & ParamType.ParamTypeNull) != 0)
			{
				s += "l";
			}
			if ((type & ParamType.ParamTypeArray) != 0)
			{
				s += "a";
			}
			if ((type & ParamType.ParamTypeObject) != 0)
			{
				s += "o";
			}
			if ((type & ParamType.ParamTypeFunc) != 0)
			{
				s += "f";
			}
			if ((type & ParamType.ParamTypeJSON) != 0)
			{
				s += "j";
			}
			if ((type & ParamType.ParamTypeAny) != 0)
			{
				s += "x";
			}

			if (s.Length > 1) 
			{
				s = "(" + s + ")";
			}

			return s;
		}

		internal static bool TryParseParamOpt(char r, out ParamOpt opt)
		{
			switch (r)
			{
			case '?':
				opt = ParamOpt.ParamOptional;
				return true;
			case '+':
				opt = ParamOpt.ParamVariadic;
				return true;
			case '-':
				opt = ParamOpt.ParamContextable;
				return true;
			default:
				opt = ParamOpt.None;
				return false;
			}
		}

		internal static string ParamOptToString(this ParamOpt opt)
		{
			switch (opt) 
			{
			case ParamOpt.None:
				return "";
			case ParamOpt.ParamOptional:
				return "?";
			case ParamOpt.ParamVariadic:
				return "+";
			case ParamOpt.ParamContextable:
				return "-";
			default:
				throw new Exception("Unexpected param opt " + opt);
			}
		}
	}
}
