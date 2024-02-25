using System.Collections.Generic;

namespace ScriptDed; 

public enum TokenType {
	None,
	EndOfFile,
	Newline,

	String,
	Number,
	Identifier,

	This,
	On,
	End,
	Is,
	Then,
	If,
	Else,
	Run,
	Global,
	True,
	False,
	NullVal,
	Var,
	Return,
	At,

	Dot,
	Colon,
	Comma,
	Semicolon,
	QuestionMark,
	LParenth,
	RParenth,
	LBrace,
	RBrace,
	LBracket,
	RBracket,

	Assign,
	AssignPlus,
	AssignMinus,
	AssignMul,
	AssignDiv,
        
	Band,
	Bor,
	Gt,
	Lt,
	Gte,
	Lte,
	Eq,
	Neq,
	Plus,
	Minus,
	Div,
	Mul,
	Not,
}

public class Token {
	public TokenType Type;
	public Position Position;
	public string Val;

	public bool IsOp () {
		return OperatorPrecedences.ContainsKey(Type);
	}

	public int OpPrec () {
		return OperatorPrecedences[Type];
	}

	public static Dictionary<TokenType, int> OperatorPrecedences = new()
	{
		{ TokenType.AssignPlus, 0 },
		{ TokenType.AssignMinus, 0 },
		{ TokenType.AssignMul, 0 },
		{ TokenType.AssignDiv, 0 },
		{ TokenType.Band, 0 },
		{ TokenType.Bor, 0 },
		{ TokenType.Is, 10 },
		{ TokenType.Eq, 10 },
		{ TokenType.Neq, 10 },
		{ TokenType.Gt, 20 },
		{ TokenType.Lt, 20 },
		{ TokenType.Gte, 20 },
		{ TokenType.Lte, 20 },
		{ TokenType.Plus, 30 },
		{ TokenType.Minus, 30 },
		{ TokenType.Div, 40 },
		{ TokenType.Mul, 40 },
	};

	public static Dictionary<string, TokenType> StrToTokenType = new()
	{
		{ "on", TokenType.On },
		{ "end", TokenType.End },
		{ "if", TokenType.If },
		{ "else", TokenType.Else },
		{ "run", TokenType.Run },
		{ "global", TokenType.Global },
		{ "this", TokenType.This },
		{ "true", TokenType.True },
		{ "false", TokenType.False },
		{ "null", TokenType.NullVal },
		{ "is", TokenType.Is },
		{ "then", TokenType.Then },
		{ "var", TokenType.Var },
		{ "return", TokenType.Return },

		{ "[", TokenType.LBracket },
		{ "]", TokenType.RBracket },
		{ "{", TokenType.LBrace },
		{ "}", TokenType.RBrace },
		{ "(", TokenType.LParenth },
		{ ")", TokenType.RParenth },
		{ "?", TokenType.QuestionMark },
            
		{ "=", TokenType.Assign },
		{ "+=", TokenType.AssignPlus },
		{ "-=", TokenType.AssignMinus },
		{ "*=", TokenType.AssignMul },
		{ "/=", TokenType.AssignDiv },
            
		{ ".", TokenType.Dot },
		{ ",", TokenType.Comma },
		{ ">", TokenType.Gt },
		{ "<", TokenType.Lt },
		{ ">=", TokenType.Gte },
		{ "<=", TokenType.Lte },
		{ "==", TokenType.Eq },
		{ "!=", TokenType.Neq },
		{ "+", TokenType.Plus },
		{ "-", TokenType.Minus },
		{ "/", TokenType.Div },
		{ "*", TokenType.Mul },
		{ "||", TokenType.Bor },
		{ "or", TokenType.Bor },
		{ "&&", TokenType.Band },
		{ "and", TokenType.Band },
		{ "not", TokenType.Not },
		{ "!", TokenType.Not },
		{ "@", TokenType.At }
	};

	public bool IsAssign() {
		return Is(TokenType.AssignPlus, TokenType.AssignMinus, TokenType.AssignMul, TokenType.Assign,
			TokenType.AssignDiv, TokenType.Assign);
	}

	public bool Is(params TokenType[] types) {
		foreach (var type in types) {
			if (type == this.Type) {
				return true;
			}
		}

		return false;
	}
}