using System;
using System.Collections.Generic;

namespace Jsonata.Net.Native.Parsing
{
    internal sealed class Lexer
    {
        private readonly string m_queryText;

        private int CurrentPos { get; set; }
        private int StartPos { get; set; }

		// symbols1 maps 1-character symbols to the corresponding
		// token types.
		private readonly Dictionary<char, TokenType> m_symbols1 = new Dictionary<char, TokenType>() {
			{ '[', TokenType.typeBracketOpen },
			{ ']', TokenType.typeBracketClose },
			{ '{', TokenType.typeBraceOpen },
			{ '}', TokenType.typeBraceClose },
			{ '(', TokenType.typeParenOpen },
			{ ')', TokenType.typeParenClose },
			{ '.', TokenType.typeDot },
			{ ',', TokenType.typeComma },
			{ ';', TokenType.typeSemicolon },
			{ ':', TokenType.typeColon },
			{ '?', TokenType.typeCondition },
			{ '+', TokenType.typePlus },
			{ '-', TokenType.typeMinus },
			{ '*', TokenType.typeMult },
			{ '/', TokenType.typeDiv },
			{ '%', TokenType.typeMod },
			{ '|', TokenType.typePipe },
			{ '=', TokenType.typeEqual },
			{ '<', TokenType.typeLess },
			{ '>', TokenType.typeGreater },
			{ '^', TokenType.typeSort },
			{ '&', TokenType.typeConcat },
		};


		// symbols2 maps 2-character symbols to the corresponding
		// token types.
		private readonly Dictionary<char, ValueTuple<char, TokenType>> m_symbols2 = new Dictionary<char, ValueTuple<char, TokenType>>() {
			{ '!', ('=', TokenType.typeNotEqual) },
			{ '<', ('=', TokenType.typeLessEqual) },
			{ '>', ('=', TokenType.typeGreaterEqual) },
			{ '.', ('.', TokenType.typeRange) },
			{ '~', ('>', TokenType.typeApply) },
			{ ':', ('=', TokenType.typeAssign) },
			{ '*', ('*', TokenType.typeDescendent) },
		};

		private static bool isDigit(char r)
		{
			return r >= '0' && r <= '9';
		}

		private static bool isNonZeroDigit(char r)
		{
			return r >= '1' && r <= '9';
		}

		private static bool isWhitespace(char c)
        {
			return (c == ' ' || c == '\t' || c == '\r' || c == '\n' || c == 'v');
		}

		public Lexer(string queryText)
        {
            this.m_queryText = queryText;
        }

		private Token eof()
		{
			return new Token(TokenType.typeEOF, null, this.CurrentPos);
		}

		internal Token next(bool allowRegex)
        {
			this.skipWhitespace();

			char? chOrNull = this.nextRune();
			if (chOrNull == null) 
			{
				return this.eof();
			};

			char ch = chOrNull.Value;
			if (allowRegex && ch == '/')
			{
				this.ignore();
				throw new NotImplementedException();
				//return this.scanRegex(ch);
			}

			ValueTuple<char, TokenType> symbol2;
			if (this.m_symbols2.TryGetValue(ch, out symbol2))
            {
				if (this.acceptRune(symbol2.Item1))
                {
					return this.newToken(symbol2.Item2);
                }
            }

			if (this.m_symbols1.TryGetValue(ch, out TokenType tt))
            {
				return this.newToken(tt);
            }

			if (ch == '"' || ch == '\'') 
			{
				this.ignore();
				return this.scanString(ch);
			}

			if (ch >= '0' && ch <= '9') 
			{
				this.backup();
				return this.scanNumber();
			}

			if (ch == '`') 
			{
				this.ignore();
				return this.scanEscapedName(ch);
			}

			this.backup();
			return this.scanName();
		}

		// scanName reads from the current position and returns a name,
		// variable, or keyword token.
		private Token scanName()
        {
			bool isVar = this.acceptRune('$');
			if (isVar) 
			{
				this.ignore();
			}

			while (true)
			{
				char? chOrNull = this.nextRune();
				if (chOrNull == null)
                {
					break;
                }

				// Stop reading if we hit whitespace...
				if (isWhitespace(chOrNull.Value)) 
				{
					this.backup();
					break;
				}

				// ...or anything that looks like an operator.
				if (this.m_symbols1.ContainsKey(chOrNull.Value) || this.m_symbols2.ContainsKey(chOrNull.Value)) 
				{
					this.backup();
					break;
				}

				if (chOrNull.Value == '@' || chOrNull.Value == '#')
				{
					//see https://github.com/jsonata-js/jsonata/pull/371
					//#### 1.7.0 Milestone Release
					// -New syntax(`@` operator) to support cross - referencing and complex data joins(issue #333)
					// -New syntax(`#` operator) to get current context position in sequence (issue #187)
					throw new NotImplementedException("Not supported yet: " + chOrNull.Value);
				}
			}

			Token result;
			if (isVar)
			{
				result = this.newToken(TokenType.typeVariable);
			}
			else if (this.lookupKeyword(this.m_queryText[this.StartPos .. this.CurrentPos], out TokenType tt))
			{
				result = this.newToken(tt);
			}
			else
            {
				result = this.newToken(TokenType.typeName);
			}
			return result;
		}

        private bool lookupKeyword(string s, out TokenType tt)
        {
			switch (s) 
			{
			case "and":
				tt = TokenType.typeAnd;
				return true;
			case "or":
				tt = TokenType.typeOr;
				return true;
			case "in":
				tt = TokenType.typeIn;
				return true;
			case "true":
			case "false":
				tt = TokenType.typeBoolean;
				return true;
			case "null":
				tt = TokenType.typeNull;
				return true;
			default:
				tt = TokenType.typeError;
				return false;
			}
		}

        // scanEscapedName reads a field name from the current position
        // and returns a name token. The opening quote has already been
        // consumed.
        private Token scanEscapedName(char quoteChar)
        {
			while (true)
			{
				char? chOrNull = this.nextRune();
				if (chOrNull == null || chOrNull.Value == '\n')
				{
					throw new ErrUnterminatedName(this.m_queryText.Substring(this.StartPos));
				};
				if (chOrNull.Value == quoteChar)
				{
					break;
				}
			}

			this.backup();
			Token result = this.newToken(TokenType.typeNameEsc);
			this.acceptRune(quoteChar);
			this.ignore();
			return result;
		}

        

		private Token scanNumber()
        {
			// JSON does not support leading zeroes. The integer part of
			// a number will either be a single zero, or a non-zero digit
			// followed by zero or more digits.
			if (!this.acceptRune('0')) 
			{
				this.accept(isNonZeroDigit);
				this.acceptAll(isDigit);
			}
			if (this.acceptRune('.'))
			{
				if (!this.acceptAll(isDigit)) 
				{
					// If there are no digits after the decimal point,
					// don't treat the dot as part of the number. It
					// could be part of the range operator, e.g. "1..5".
					this.backup();
					return this.newToken(TokenType.typeNumber);
				}
			}
			if (this.acceptRunes2('e', 'E')) 
			{
				this.acceptRunes2('+', '-');
				this.acceptAll(isDigit);
			}
			return this.newToken(TokenType.typeNumber);
		}

        private Token scanString(char quoteChar)
        {
			while (true) 
			{
				char? chOrNull = this.nextRune();
				if (chOrNull == null)
                {
					throw new ErrUnterminatedString(this.m_queryText.Substring(this.StartPos));
				};
				if (chOrNull.Value == quoteChar)
                {
					break;
                }
				if (chOrNull.Value == '\\')
                {
					chOrNull = this.nextRune();
					if (chOrNull == null)
					{
						throw new ErrUnterminatedString(this.m_queryText.Substring(this.StartPos));
					};
				}
			}
			this.backup();
			Token result = this.newToken(TokenType.typeString);
			this.acceptRune(quoteChar);
			this.ignore();
			return result;
		}

        private void skipWhitespace()
        {
			this.acceptAll(isWhitespace);
			this.ignore();
		}

        private void backup()
        {
			--this.CurrentPos;
        }

        private bool acceptRune(char rune)
        {
			if (this.CurrentPos < this.m_queryText.Length 
				&& this.m_queryText[this.CurrentPos] == rune)
            {
				nextRune();
				return true;
            }
			return false;
        }

		private bool acceptRunes2(char r1, char r2)
		{
			if (this.CurrentPos < this.m_queryText.Length
				&& (this.m_queryText[this.CurrentPos] == r1
					|| this.m_queryText[this.CurrentPos] == r2
				)
			)
			{
				nextRune();
				return true;
			}
			return false;
		}

		private bool accept(Func<char, bool> isValid)
		{
			if (this.CurrentPos < this.m_queryText.Length
				&& isValid(this.m_queryText[this.CurrentPos]))
			{
				nextRune();
				return true;
			}
			return false;
		}

		private bool acceptAll(Func<char, bool> isValid)
		{
			bool result = false;
			while (this.accept(isValid))
            {
				result = true;
            }
			return result;
		}

		private Token newToken(TokenType type)
        {
			Token result = new Token(type, this.m_queryText[this.StartPos..this.CurrentPos], this.StartPos);
			this.StartPos = this.CurrentPos;
			return result;
		}

        private char? nextRune()
        {
			if (this.CurrentPos >= this.m_queryText.Length) 
			{
				return null;
			}
			char r = this.m_queryText[this.CurrentPos];
			this.CurrentPos += 1;
			return r;
		}

        private void ignore()
        {
			this.StartPos = this.CurrentPos;
		}
    }
}