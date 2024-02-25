using System;
using System.Collections.Generic;

namespace ScriptDed; 

public class Parser : Pass {
	Lexer _lexer;

	public Parser(string filename, string sourceCode) {
		_lexer = new Lexer(filename, sourceCode);
	}

	public Ast Parse() {
		AstBlock block = new AstBlock();

		var startedActualCode = false;
		while(!_lexer.IsEnd()) {
			Ast childAst;

			if(IsNext(TokenType.EndOfFile)) {
				break;
			}

			if(IsNext(TokenType.Global)) {
				if(startedActualCode) {
					RaiseError("global should be at the top of the file", _lexer.PeekToken());
				}
			} else {
				startedActualCode = true;
			}

			childAst = ParsePrimary(false);
                
			EeNewline();
                
			block.Children.Add(childAst);
		}

		return block;
	}

	AstGlobal ParseGlobal() {
		var astGlobal = new AstGlobal();

		Expect(TokenType.Identifier, "expected an identifier after global keyword");

		astGlobal.Identifiers.Add(ParseIdentifier());

		var useComma = IsNextAndEat(TokenType.Comma);

		while(!IsNext(TokenType.Newline)) {
			astGlobal.Identifiers.Add(ParseIdentifier());

			if(!IsNext(TokenType.Newline)) {
				if(useComma) {
					ExpectAndEat(TokenType.Comma, "expected a comma after an identifier");
				}
			} else {
				break;
			}
		}

		return astGlobal;
	}

	AstIdentifier ParseIdentifier () {
		var token = _lexer.GetToken();

		var ident = new AstIdentifier (token.Val);
		ident.Position = token.Position;

		if(IsNextAndEat(TokenType.Dot)) {
			Expect(TokenType.Identifier, "expected an identifier");

			ident.SubField = ParseIdentifier();
		}

		return ident;
	}

	AstBlock ParseOpenBlock () {
		var block = new AstBlock();
		while(!IsNextAndEat(TokenType.End)) {
			var childAst = ParsePrimary(false);

			block.Children.Add(childAst);
                
			EeNewline();
		}
		return block;
	}

	AstBlock ParseOpenIfBlock () {
		var block = new AstBlock();
		while(!IsNext(TokenType.If) && !IsNext(TokenType.Else) && !IsNext(TokenType.End)) {
			var childAst = ParsePrimary(false);

			block.Children.Add(childAst);
                
			EeNewline();
		}
		return block;
	}

	AstCall ParseCall (AstIdentifier name) {
		var useParenth = IsNextAndEat(TokenType.LParenth);

		var astCall = new AstCall(name);

		Position firstArgPos = new Position();

		var useComma = useParenth;
		var hasExpr = false;
		var firstArg = true;
		while(true) {
			if(IsNext(TokenType.Newline)) {
				if(useParenth) {
					RaiseError("expected an ending parenthesis", _lexer.PeekToken());
				}
				break;
			} else if(IsNextAndEat(TokenType.RParenth)) {
				break;
			}

			var arg = ParseExpr(false);
			astCall.Args.Add(arg);

			if(arg is AstExpr) {
				useComma = true;
				hasExpr = true;
			}

			var argPos = _lexer.PeekToken().Position;

			if(firstArg) {
				firstArgPos = argPos;
			}

			if(useComma){
				if(!IsNext(TokenType.Newline) && !IsNextAndEat(TokenType.Comma)) {
					if(useParenth) {
						if(!IsNext(TokenType.RParenth)) {
							ExpectAndEat(TokenType.Comma, "must use a comma after arguments when using parenthesis around the call");
						}
					} else if (hasExpr) {
						ExpectAndEat(TokenType.Comma, "must use a comma after arguments when there is an expression in the arguments", firstArgPos);
					} else {
						ExpectAndEat(TokenType.Comma, "expected a comma");
					}
				}
			} else if(IsNextAndEat(TokenType.Comma)) {
				useComma = true;
			}

			firstArg = false;
		}

		return astCall;
	}

	Ast ParseExpr (bool isIfExpr) {
		return ParseExprPrimary(ParsePrimary(true), 0, isIfExpr);
	}

	Ast ParseExprPrimary(Ast lhs, int minimumPrecedence, bool isIfExpr) {
		var nextToken = _lexer.PeekToken();

		if (isIfExpr && lhs is AstIdentifier && (!nextToken.IsOp() || nextToken.Is(TokenType.Band, TokenType.Bor))) {
			var newAstExpr = new AstExpr();

			newAstExpr.Lhs = lhs;
			newAstExpr.Rhs = new AstBool(true);
			newAstExpr.Rhs.Position = lhs.Position;
			newAstExpr.Op = TokenType.Eq;
			newAstExpr.Position = lhs.Position;
                
			lhs = newAstExpr;
		}
            
		while(nextToken.IsOp() && nextToken.OpPrec() >= minimumPrecedence) {
			var op = nextToken;

			_lexer.GetToken();

			var rhs = ParsePrimary(true);

			nextToken = _lexer.PeekToken();

			while(nextToken.IsOp() && op.OpPrec() < nextToken.OpPrec()) {
				rhs = ParseExprPrimary(rhs, op.OpPrec() + 1, isIfExpr);
				nextToken = _lexer.PeekToken();
			}

			var expr = new AstExpr();
			expr.Lhs = lhs;
			expr.Rhs = rhs;
			expr.Op = op.Type;
			expr.Position = lhs.Position;
			expr.Position.ColTo = rhs.Position.ColTo;

			lhs = expr;
		}

		return lhs;
	}

	AstAssign ParseAssignment (AstIdentifier astIdentifier) {
		var assignType = _lexer.GetToken();
	        
		var expr = ParseExpr(false);
	        
		return new AstAssign(astIdentifier, expr, assignType.Type);
	}

	AstIf ParseIf () {
		var astIf = new AstIf();
		var rootAstIf = astIf;
		while(true) {
			if(IsNextAndEat(TokenType.If)) {
				astIf.Expr = ParseExpr(true);

				Optional(TokenType.Then);
			}

			EeNewline();

			astIf.Block = ParseOpenIfBlock();

			if(!IsNextAndEat(TokenType.Else)) {
				break;
			}

			astIf.NextIf = new AstIf();
			astIf = astIf.NextIf;
		}

		ExpectAndEat(TokenType.End, "expected an end to the if statement");

		return rootAstIf;
	}

	AstRun ParseRun(bool isExpr) {
		var token = _lexer.GetToken();

		var astRun = new AstRun();

		if(IsNext(TokenType.Identifier)) {
			if(isExpr) {
				RaiseError("cannot run an identifier in an expression", token.Position);
			}

			var runIdentifier = ParseIdentifier();

			if (!IsNext(TokenType.Newline) || IsNext(TokenType.LParenth)) {
				astRun.Run = ConvertToBlock(ParseCall(runIdentifier));
			} else {
				astRun.Run = runIdentifier;
			}
		        
			Optional(TokenType.End);
		} else if(IsNextAndEat(TokenType.Newline)) {
			astRun.Run = ParseOpenBlock();
		} else {
			RaiseError("run should have a code block or an identifier as the parameter", token.Position);
		}

		return astRun;
	}

	AstBlock ConvertToBlock(Ast ast) {
		if (ast is not AstBlock) {
			var astBlock = new AstBlock();
			astBlock.Children.Add(ast);
			return astBlock;
		}
		return (AstBlock) ast;
	}
        
	Ast ParsePrimary (bool isExpr) {
		var token = _lexer.PeekToken();

		switch(token.Type) {
			case TokenType.On: {
				_lexer.GetToken();

				Expect(TokenType.Identifier, "expected an identifier");

				var handle = ParseIdentifier();

				Expect(TokenType.Run, "expected a run");

				var runBlock = ParseRun(false);

				var astOn = new AstOn(handle, runBlock);

				astOn.Position = token.Position;

				return astOn;
			}
			case TokenType.QuestionMark: {
				_lexer.GetToken();
                    
				ExpectAndEat(TokenType.LParenth, "expected a starting parenthesis when using single line if statements");

				var expr = ParseExpr(true);
                    
				ExpectAndEat(TokenType.RParenth, "expected an ending parenthesis after the expression when using single line if statements");

				Optional(TokenType.Newline);
                    
				var astIf = new AstIf();
				astIf.Expr = expr;
				astIf.Block = ConvertToBlock(ParsePrimary(false));
				return astIf;
			} break;
			case TokenType.Not: {
				_lexer.GetToken();
                    
				var astExpr = new AstExpr();
				astExpr.Lhs = ParsePrimary(true);
				astExpr.Rhs = new AstBool(false);
				astExpr.Rhs.Position = token.Position;
				astExpr.Op = TokenType.Eq;
				astExpr.Position = token.Position;
                    
				return astExpr;
			}
			case TokenType.Identifier:
			case TokenType.This: {
				var identifier = ParseIdentifier();

				if(isExpr && !IsNext(TokenType.LParenth)) {
					return identifier;
				}
                    
				if(_lexer.PeekToken().IsAssign()) {
					var astAssign = ParseAssignment(identifier);

					astAssign.Position = identifier.Position;
					return astAssign;
				}

				var call = ParseCall(identifier);

				call.Position = identifier.Position;

				return call;
			}
			case TokenType.At: {
				var annotations = new List<AstAnnotation>();
				while (IsNextAndEat(TokenType.At)) {
					Expect(TokenType.Identifier, "need the annotation name as an identifier");
                        
					var identifier = ParseIdentifier();
                        
					annotations.Add(new AstAnnotation {
						Name = identifier.Name,
						Position = identifier.Position
					});
				}

				var realAst = ParsePrimary(isExpr);
				realAst.Annotations = annotations;
				return realAst;
			} break;
			case TokenType.LBrace: {
				_lexer.GetToken();
                    
				EeNewline();

				var astObject = new AstObject();
				while (!IsNextAndEat(TokenType.RBrace)) {
					Expect(TokenType.Identifier, "expected the field identifier");

					var identifier = _lexer.GetToken();
                        
					ExpectAndEat(TokenType.Assign, "expected the assign for the value");

					var expr = ParseExpr(false);
                        
					EeNewline();

					astObject.Fields[identifier.Val] = expr;
				}
                    
				return astObject;
			} break;
			case TokenType.Run: {
				return ParseRun(isExpr);
			}
			case TokenType.String: {
				_lexer.GetToken();
				var astStr = new AstString(token.Val);

				astStr.Position = token.Position;

				return astStr;
			} 
			case TokenType.Number: {
				_lexer.GetToken();

				var astNum = new AstNumber(Double.Parse(token.Val));

				astNum.Position = token.Position;

				return astNum;
			}
			case TokenType.True:
			case TokenType.False: {
				_lexer.GetToken();

				var astBool = new AstBool(token.Type == TokenType.True);
				astBool.Position = token.Position;
				return astBool;
			}
			case TokenType.If: {
				return ParseIf();
			}
			case TokenType.Global: {
				if (isExpr) {
					RaiseError("cannot use global keyword in an expression", token);
				}
	                
				_lexer.GetToken();
	                
				var astGlobal = new AstGlobal();

				var isUsingComma = false;
				var isFirst = true;
				while (!IsNext(TokenType.Newline)) {
					Expect(TokenType.Identifier, "expected an identifier to identify the global variable");

					var astIdentifier = ParseIdentifier();

					if (astGlobal.Identifiers.Exists(identifier => identifier.Name == astIdentifier.Name)) {
						RaiseError($"identifier {astIdentifier.Name} is already mentioned earlier in the global", astIdentifier.Position);
					}
		                
					astGlobal.Identifiers.Add(astIdentifier);

					if (IsNextAndEat(TokenType.Comma)) {
						if (!isFirst && !isUsingComma) {
							RaiseError("separate the global identifiers either with a comma or space, but you cant mix both", _lexer.PeekToken());
						}
			                
						isUsingComma = true;
					} else if (!IsNext(TokenType.Newline) && !isFirst) {
						if (isUsingComma) {
							RaiseError("separate the global identifiers either with a comma or space, but you cant mix both", _lexer.PeekToken());
						}
					}

					isFirst = false;
				}
	                
				return astGlobal;
			} 
			default: {
				RaiseError("unhandled token type!", token.Position);
			} break;
		}

		return null;
	}

	bool IsNext(params TokenType[] types) {
		return _lexer.PeekToken().Is(types);
	}

	bool IsNextAndEat(params TokenType[] types) {
		if(IsNext(types)) {
			_lexer.GetToken();
			return true;
		}
		return false;
	}

	bool Optional(params TokenType[] types) {
		return IsNextAndEat(types);
	}
        
	void Expect(TokenType type, string errorMsg) {
		if(!IsNext(type)) {
			RaiseError(errorMsg, _lexer.PeekToken());
		}
	}

	void ExpectAndEat(TokenType type, string errorMsg) {
		if(!IsNextAndEat(type)) {
			RaiseError(errorMsg, _lexer.PeekToken());
		}
	}

	void ExpectAndEat(TokenType type, string errorMsg, Position pos) {
		if(!IsNextAndEat(type)) {
			RaiseError(errorMsg, pos);
		}
	}

	void EeNewline () {
		ExpectAndEat(TokenType.Newline, "expected a newline");
	}
}