using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jsonata.Net.Native.Parsing
{
    internal enum TokenType
    {
		typeEOF,
		typeError,

		typeString,  // string literal, e.g. "hello"
		typeNumber,   // number literal, e.g. 3.14159
		typeBoolean,  // true or false
		typeNull,     // null
		typeName,     // field name, e.g. Price
		typeNameEsc,  // escaped field name, e.g. `Product Name`
		typeVariable, // variable, e.g. $x
		typeRegex,    // regular expression, e.g. /ab+/

		// Symbol operators
		typeBracketOpen,
		typeBracketClose,
		typeBraceOpen,
		typeBraceClose,
		typeParenOpen,
		typeParenClose,
		typeDot,
		typeComma,
		typeColon,
		typeSemicolon,
		typeCondition,
		typePlus,
		typeMinus,
		typeMult,
		typeDiv,
		typeMod,
		typePipe,
		typeEqual,
		typeNotEqual,
		typeLess,
		typeLessEqual,
		typeGreater,
		typeGreaterEqual,
		typeApply,
		typeSort,
		typeConcat,
		typeRange,
		typeAssign,
		typeDescendant,

		// Keyword operators
		typeAnd,
		typeOr,
		typeIn,
	}
}
