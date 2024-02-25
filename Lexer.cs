using System;
using System.IO;

namespace ScriptDed; 

public class Lexer {
	Position _position;
	bool _lastOneWasNewLine = true;

	Token _lastToken = new Token {
		Type = TokenType.None
	};

	public Lexer(string filename, string sourceCode) {
		sourceCode += '\n';

		_position = new Position(filename, sourceCode);
	}

	public bool IsEnd () {
		return _position.ColFrom >= _position.SourceCode.Length;
	}

	char GetChr () {
		var chr = _position.SourceCode[_position.ColFrom];

		_position.ColFrom++;

		if(chr == '\n') {
			_position.Row++;
			_position.Col = 1;
		} else {
			_position.Col++;
		}

		return chr;
	}

	char PeekChr (int p = 0) {
		if(_position.ColFrom >= _position.SourceCode.Length) {
			return '\0';
		}

		return _position.SourceCode[_position.ColFrom + p];
	}

	public Token GetNone () {
		return new Token { 
			Type = TokenType.None 
		};
	}

	void EatChr (int c = 0) {
		for(int i = 0; i < c; i++) {
			GetChr();
		}
	}

	public Token PeekToken () {
		if(IsEnd()) {
			return GetNone();
		}

		if(_lastToken.Type != TokenType.None) {
			return _lastToken;
		}

		_lastToken = GetToken();

		return _lastToken;
	}

	public Token GetToken () {
		if(_lastToken.Type != TokenType.None) {
			var tok = _lastToken;
			_lastToken = GetNone();
			return tok;
		}

		while(true) {
			if(Char.IsWhiteSpace(PeekChr())) {
				while(Char.IsWhiteSpace(PeekChr())) {
					if (!_lastOneWasNewLine && PeekChr() == '\n') {
						_lastOneWasNewLine = true;

						return new Token {
							Type = TokenType.Newline,
							Position = _position
						};
					}

					GetChr();
				}
			} else if(PeekChr() == '#') {
				while(PeekChr() != '\n') {
					GetChr();
				}
			} else {
				break;
			}
		}

		if(IsEnd()) {
			return new Token {
				Type = TokenType.EndOfFile,
				Position = _position
			};
		}

		_lastOneWasNewLine = false;

		Position newPosition;

		if(Char.IsLetter(PeekChr())) {
			var identifier = "";

			while(Char.IsLetterOrDigit(PeekChr()) || PeekChr() == '_') {
				identifier += GetChr();
			}

			var type = TokenType.Identifier;

			if(Token.StrToTokenType.ContainsKey(identifier)) {
				type = Token.StrToTokenType[identifier];
			}

			newPosition = _position;
			newPosition.ColTo = newPosition.ColFrom + identifier.Length;

			return new Token {
				Type = type,
				Position = newPosition,
				Val = identifier
			};
		} else if (Char.IsDigit(PeekChr()) || PeekChr() == '-') {
			var number = "";

			if(PeekChr() == '-') {
				number += GetChr();
			}

			while(Char.IsDigit(PeekChr()) || PeekChr() == '.') {
				number += GetChr();
			}

			newPosition = _position;
			newPosition.ColTo = newPosition.ColFrom + number.Length;

			return new Token {
				Type = TokenType.Number,
				Position = newPosition,
				Val = number,
			};
		} else if (PeekChr() == '"') {
			var str = "";

			GetChr();

			while(PeekChr() != '"') {
				str += GetChr();
			}

			GetChr();

			newPosition = _position;
			newPosition.ColTo = newPosition.ColFrom + str.Length;

			return new Token {
				Type = TokenType.String,
				Position = newPosition,
				Val = str
			};
		}

		var chr = GetChr();
		var chrStr = "" + chr;

		while(true) {
			int c = 0;
			var found = false;
			foreach(var str in Token.StrToTokenType) {
				if(str.Key.StartsWith(chrStr + PeekChr(c))) {
					found = true;
					chrStr += PeekChr(c++);
				}
			}

			if(!found) {
				break;
			}
		}

		EatChr(chrStr.Length - 1);

		if(!Token.StrToTokenType.ContainsKey(chrStr)) {
			throw new CompileError("Unexpected token.");
		}

		newPosition = _position;
		newPosition.ColTo = newPosition.ColFrom + chrStr.Length;

		return new Token {
			Type = Token.StrToTokenType[chrStr],
			Position = newPosition,
			Val = chrStr
		};
	}
}