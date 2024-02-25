using System;
using System.Collections.Generic;

namespace ScriptDed; 

public class Compiler : Pass {
	public Parser Parser;
	public CompUnit Compunit = new CompUnit();
	public Stack<Block> BlockStack = new();

	static Dictionary<TokenType, OpCode> _opsToOpcodes = new() {
		{ TokenType.Eq, OpCode.Eq },
		{ TokenType.Neq, OpCode.Neq },
		{ TokenType.Gt, OpCode.Gt },
		{ TokenType.Lt, OpCode.Lt },
		{ TokenType.Gte, OpCode.Gte },
		{ TokenType.Lte, OpCode.Lte },
		{ TokenType.Plus, OpCode.Plus },
		{ TokenType.Minus, OpCode.Minus },
		{ TokenType.Div, OpCode.Div },
		{ TokenType.Mul, OpCode.Mul },
		{ TokenType.Band, OpCode.Band },
		{ TokenType.Bor, OpCode.Bor },
		{ TokenType.Is, OpCode.Eq }
	};

	public Compiler (string filename, string sourceCode) {
		Compunit.Name = filename;
		Compunit.Filename = filename;
		Parser = new Parser(filename, sourceCode);
	}

	public static CompUnit CompileUnit(string filename, string sourceCode) {
		var compiler = new Compiler(filename, sourceCode);
		return compiler.Compile();
	}

	public CompUnit Compile() {
		var ast = Parser.Parse();

		GenAst(ast, false); 
            
		return Compunit;
	}

	public int NewBlock() {
		var newBlockId = Compunit.Blocks.Count;
		var block = new Block();
		Compunit.Blocks.Add(block);
		BlockStack.Push(block);
		return newBlockId;
	}

	public void PopBlock() { 
		BlockStack.Pop(); 
	}

	public int GenBlock(AstBlock astBlock) {
		var blockId = NewBlock();

		foreach (var childAst in astBlock.Children) {
			GenAst(childAst, false);
		}

		AddOp(OpCode.Ret);

		PopBlock();

		return blockId;
	}

	public int GenBlockContents(AstBlock astBlock) {
		var startip = CurrentIP();
		foreach (var childAst in astBlock.Children) {
			GenAst(childAst, false);
		}
		return CurrentIP() - startip;
	}

	public int GenIdentifier(AstIdentifier astIdentifier) {
		AddOp(OpCode.Const, FetchConst(astIdentifier.Name));

		int parts = 1;
		if (astIdentifier.SubField != null) {
			AddOp(OpCode.Fetch);

			parts += GenIdentifier(astIdentifier.SubField);
		}

		return parts;
	}

	public void GenFetchIdentifier(AstIdentifier astIdentifier, bool isSubField) {
		AddOp(OpCode.Const, FetchConst(astIdentifier.Name));

		if (isSubField) {
			AddOp(OpCode.FetchField);
		} else {
			AddOp(OpCode.Fetch);
		}
            
		if (astIdentifier.SubField != null) {
			GenFetchIdentifier(astIdentifier.SubField, true);
		}
	}

	public int GenCall(AstCall astCall, bool expectedToReturn = false) {
		GenFetchIdentifier(astCall.Name, false);

		foreach (var arg in astCall.Args) {
			GenAst(arg, true);
		}

		AddOp(OpCode.Call, astCall.Args.Count, expectedToReturn ? 1 : 0);

		return 0;
	}

	public int CurrentIP() {
		if (BlockStack.Count == 0) {
			return 0;
		}

		return CurrentBlock().Instructions.Count - 1;
	}

	public void GenAnnotations(List<AstAnnotation> annotations) {
		foreach (var annotation in annotations) {
			AddOp(OpCode.Annotation, FetchConst(annotation.Name));
		}
	}

	public int GenAst(Ast ast, bool isExpr) {
		var startip = CurrentIP();

		switch (ast) {
			case AstBlock astBlock: {
				int blockId = GenBlock(astBlock);

				if (isExpr) {
					AddOp(OpCode.PushBlock, blockId);
				}
			} break;
			case AstOn astOn: {
				var handle = astOn.Handle;
				var hasAddedCurrentObjectName = false;
				var first = true;
				while (handle != null) {
					AddOp(OpCode.Const, FetchConst(handle.Name));

					if (!hasAddedCurrentObjectName) {
						hasAddedCurrentObjectName = true;
						if (!Compunit.AddedEventsToObjectNamed.Contains(handle.Name)) {
							Compunit.AddedEventsToObjectNamed.Add(handle.Name);
						}
					}
					
					if (first) {
						AddOp(OpCode.Fetch);
					} else if (handle.SubField != null) {
						AddOp(OpCode.FetchField);
					}

					first = false;
					handle = handle.SubField;
				}
                
				GenAst(astOn.Run, true);
                
				AddOp(OpCode.On);
			} break;
			case AstAssign astAssign: {
				var parts = GenIdentifier(astAssign.Ident);

				if (!Compunit.VariablesAssigned.Contains(astAssign.Ident.Name)) {
					Compunit.VariablesAssigned.Add(astAssign.Ident.Name);
				}
                
				switch (astAssign.Op) {
					case TokenType.Assign:
						GenAst(astAssign.Expr, true);
						break;
					case TokenType.AssignPlus:
						GenFetchIdentifier(astAssign.Ident, false);
						GenAst(astAssign.Expr, true);
						AddOp(OpCode.Plus);
						break;
					case TokenType.AssignMinus:
						GenFetchIdentifier(astAssign.Ident, false);
						GenAst(astAssign.Expr, true);
						AddOp(OpCode.Minus);
						break;
					case TokenType.AssignMul:
						GenFetchIdentifier(astAssign.Ident, false);
						GenAst(astAssign.Expr, true);
						AddOp(OpCode.Mul);
						break;
					case TokenType.AssignDiv:
						GenFetchIdentifier(astAssign.Ident, false);
						GenAst(astAssign.Expr, true);
						AddOp(OpCode.Div);
						break;
				}
                
				if(parts > 1) {
					AddOp(OpCode.AssignField);
				} else {
					AddOp(OpCode.Assign);
				}
			} break;
			case AstExpr astExpr: {
				if (!Compiler._opsToOpcodes.ContainsKey(astExpr.Op)) {
					throw new CompileError("Unexpected opcode");
				}

				GenAst(astExpr.Lhs, true);
                
				var opcode = _opsToOpcodes[astExpr.Op];
				if(opcode == OpCode.Band) {
					var jmppos = AddOp(OpCode.Czjmp);

					var rhsamount = GenAst(astExpr.Rhs, true);

					SetOp(jmppos, OpCode.Czjmp, rhsamount + 1);
					AddOp(OpCode.Jmp, 1);
					AddOp(OpCode.PushFalse);
                    
					AddOp(opcode);
				} else if(opcode == OpCode.Bor) {
					var jmppos = AddOp(OpCode.Cjmp);

					var rhsamount = GenAst(astExpr.Rhs, true);

					SetOp(jmppos, OpCode.Cjmp, rhsamount + 1);

					AddOp(OpCode.Jmp, 1);
					AddOp(OpCode.PushTrue);

					AddOp(opcode);
				} else {
					GenAst(astExpr.Rhs, true);

					AddOp(opcode);
				}
			} break;
			case AstIdentifier astIdentifier: {
				var found = false;

				if (!Compunit.VariablesUsed.Contains(astIdentifier.Name)) {
					Compunit.VariablesUsed.Add(astIdentifier.Name);
				}

				if(astIdentifier.Name == "this" && astIdentifier.SubField != null) {
					GenIdentifier(astIdentifier);
				} else {
					GenFetchIdentifier(astIdentifier, false);
				}
			} break;
			case AstNumber astNumber: {
				var isInt = astNumber.Number % 1 == 0;

				if (isInt && astNumber.Number < 255) {
					AddOp(OpCode.PushNumber, (int) astNumber.Number);
				} else {
					AddOp(OpCode.Const,
						FetchConst(astNumber.Number)
					);
				}
			} break;
			case AstString astString: {
				AddOp(OpCode.Const,
					FetchConst(astString.Value)
				);
			} break;
			case AstCall astCall: {
				GenCall(astCall, isExpr);
			} break;
			case AstRun astRun: {
				GenAst(astRun.Run, isExpr);
			} break;
			case AstIf astIf: {
				var endJmps = new List<int>();

				var lastIfEnded = -1;
				while (astIf != null) {
					if (astIf.Expr != null) { 
						GenAst(astIf.Expr, true);
						var exprJmpPoint = AddOp(OpCode.Czjmp, -1);
			            
						GenBlockContents(astIf.Block);

						var blockJmpPoint = AddOp(OpCode.Jmp, -1);
			            
						endJmps.Add(blockJmpPoint);
			            
						SetOp(exprJmpPoint, OpCode.Czjmp, (CurrentIP() - exprJmpPoint));

						lastIfEnded = CurrentIP();
					} else {
						GenBlockContents(astIf.Block);
					}

					astIf = astIf.NextIf;
				}

				var endPoint = CurrentIP();
				for (var c = endJmps.Count - 1; c >= 0; c--) {
					SetOp(endJmps[c], OpCode.Jmp, endPoint - endJmps[c]);
				}
			} break;
			case AstBool astBool: {
				AddOp(astBool.BoolVal ? OpCode.PushTrue : OpCode.PushFalse);
			} break;
			case AstGlobal astGlobal: {
				foreach (var astIdentifier in astGlobal.Identifiers) {
					GenIdentifier(astIdentifier);
					if (!Compunit.GlobalVariables.Contains(astIdentifier.Name)) {
						Compunit.GlobalVariables.Add(astIdentifier.Name);   
					}
				}
				AddOp(OpCode.Global, astGlobal.Identifiers.Count);
			} break;
			case AstObject astObject: {
				GenAnnotations(astObject.Annotations);
                
				AddOp(OpCode.PushNewObject);

				foreach (var field in astObject.Fields) {
					AddOp(OpCode.Const, FetchConst(field.Key));

					GenAst(field.Value, true);

					AddOp(OpCode.AssignField, 1);
				}
			} break;
			default: {
				throw new CompileError("This should not happen!");
			} break;
		}

		return CurrentIP() - startip;
	}

	public int AddDebug(string text) {
		return AddOp(OpCode.Debug, FetchConst(text));
	}

	int AddOp(OpCode code, int arg1 = 0, int arg2 = 0, int arg3 = 0) {
		if(code == OpCode.Const && Compunit.Consts.Count >= 255) {
			code = OpCode.Const2;
		}

		var newInstr = new Instr { 
			Op = code, 
			Args = new byte[3]{
				(byte) arg1,
				(byte) arg2,
				(byte) arg3
			}
		};

		CurrentBlock().Instructions.Add(newInstr);

		return CurrentIP();
	}

	int SetOp(int ip, OpCode code, int arg1 = 0, int arg2 = 0, int arg3 = 0) {
		if(code == OpCode.Const && Compunit.Consts.Count >= 255) {
			code = OpCode.Const2;
		}

		CurrentBlock().Instructions[ip] = new Instr
		{
			Op = code,
			Args = new byte[3]{
				(byte) arg1,
				(byte) arg2,
				(byte) arg3
			}
		};

		return CurrentBlock().Instructions.Count - 1;
	}

	public int FetchConst(string strval) {
		for (int c = 0; c < Compunit.Consts.Count; c++) {
			var cnst = Compunit.Consts[c];
			if (cnst.Type == ValueType.String && cnst.StrValue == strval) {
				return c;
			}
		}

		var newConstItem = new Value(strval);

		var newNum = Compunit.Consts.Count;
		Compunit.Consts.Add(newConstItem);
		return newNum;
	}

	public int FetchConst(double numval) {
		for (int c = 0; c < Compunit.Consts.Count; c++) {
			var cnst = Compunit.Consts[c];
			if (cnst.Type == ValueType.Number && cnst.NumValue == numval) {
				return c;
			}
		}

		var newConstItem = new Value(numval);

		var newNum = Compunit.Consts.Count;
		Compunit.Consts.Add(newConstItem);
		return newNum;
	}

	Block CurrentBlock() {
		return BlockStack.Peek();
	}
}