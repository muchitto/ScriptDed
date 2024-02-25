using System;
using System.Collections.Generic;
using System.Linq;

namespace ScriptDed; 

public enum VMState {
	Running,
	Completed,
	Halted,
	Errored,
}

public class StackFrame {
	public int Ip;
	public Block Block;
	public ScriptObject? Obj;

	public StackFrame(Block block) {
		this.Block = block;
		this.Obj = null;
		this.Ip = 0;
	}

	public StackFrame(Block block, ScriptObject obj) {
		this.Block = block;
		this.Obj = obj;
		this.Ip = 0;
	}
}

public class VM : DataHolder {
	public VMState State = VMState.Running;
	public CompUnit Compunit;

	public List<Value> Stack = new(1024);

	public List<StackFrame> Frames = new();
	public List<Block> QueuedBlocks = new();
	public List<string> GlobalVariables = new();

	public List<ValueAnnotation> NextAnnotations = new();
        
	public event EventHandler<string> ErrorEvents;
        
	public VM() {
	}

	public VM(CompUnit compunit) {
		SetCompUnit(compunit);
	}

	public void AddDefaultErrorEvent() {
		ErrorEvents += (sender, s) => {
			PopFrame();
		};
	}

	public void SetCompUnit(CompUnit compunit) {
		this.Compunit = compunit;
		Frames.Clear();
		PurgeLocalVariables();
		NextAnnotations.Clear();
		NewFrame(compunit.Blocks[0], null);
	}

	public StackFrame CurrentFrame () {
		return Frames[Frames.Count - 1];
	}

	public StackFrame NewFrame (Block block, ScriptObject obj) {
		var frame = new StackFrame(block, obj);
		Frames.Add(frame);
		return frame;
	}

	public void PopFrame () {
		Frames.RemoveAt(Frames.Count - 1);
	}

	public Value PopStack (int num = 0) {
		var val = Stack[Stack.Count - 1 + num];
		Stack.RemoveAt(Stack.Count - 1 + num);

		return val;
	}

	public void PushStack(Value value) {
		Stack.Add(value);
	}

	public Value GetStackVal (int num = 0) {
		return Stack[Stack.Count - 1 + num];
	}

	public void RunBlock (Block block) {
		if(State != VMState.Completed) {
			return;
		}

		NewFrame(block, null);

		Run();
	}

	public bool SetGlobalFunction (string name, NativeFunc func) {
		return base.SetFunction(name, func, true);
	}

	public bool QueueBlocks (List<Block> blocks, ScriptObject obj = null) {
		if(State != VMState.Completed) {
			return false;
		}

		var firstBlock = blocks[0];
		blocks.RemoveAt(0);

		if(blocks.Count != 0) {
			foreach (var block in blocks) {
				QueuedBlocks.Add(block);
			}
		}

		NewFrame(firstBlock, obj);

		return true;
	}

	public bool CallVarEvent(string var, string eventName) {
		if(State != VMState.Completed) {
			return false;
		}

		if(!HasVar(var)) {
			return false;
		}

		if(Variables[var].Type != ValueType.Object) {
			return false;
		}

		var obj = Variables[var].ObjectValue;
		var events = obj.GetEvents(eventName);

		if(events.Count == 0) {
			return false;
		}

		QueueBlocks(new(events), obj);

		return true;
	}

	public VMState RunCallVarEvent(string var, string eventName) {
		CallVarEvent(var, eventName);
		return Run();
	}

	public void PurgeLocalVariables () {
		foreach(var var in Variables) {
			if (!var.Value.IsGlobal) {
				Variables.Remove(var.Key);
			}
		}
	}

	public VMState ContinueExecution () {
		if(State != VMState.Halted) {
			return State;
		}

		State = VMState.Running;
		return Run();
	}

	public VMState HaltExecution () {
		return State = VMState.Halted;
	}

	public VMState RaiseError(string error) {
		State = VMState.Errored;
		ErrorEvents?.Invoke(this, error);
		return State;
	}

	private void NextQueuedBlock() {
		var nextBlock = QueuedBlocks[0];
		QueuedBlocks.RemoveAt(0);
		NewFrame(nextBlock, null);
	}

	private void addAnnotationToValue(Value value) {
		foreach (var annotation in NextAnnotations) {
			value.Annotations.Add(annotation);
		}
		NextAnnotations.Clear();
	}
        
	public VMState Run () {
		if (Frames.Count == 0 && QueuedBlocks.Count == 0) {
			return VMState.Completed;
		} else if (Frames.Count == 0) {
			NextQueuedBlock();
		}
	        
		State = VMState.Running;

		while(State == VMState.Running) {
			var nextInstr = CurrentFrame().Block.Instructions[CurrentFrame().Ip++];
			var firstArg = nextInstr.Args[0];

			switch(nextInstr.Op) {
				case OpCode.Const: {
					var cnst = Compunit.Consts[firstArg];
					PushStack(cnst);
				} break;
				case OpCode.PushBlock: {
					var block = Compunit.Blocks[firstArg];

					var val = new Value(block);

					addAnnotationToValue(val);
                        
					PushStack(val);
				} break;
				case OpCode.Fetch: {
					var name = PopStack();

					if(!name.Is(ValueType.String)) {
						return RaiseError("fetch failed, because the top stack item is not a string");
					}

					if(!Variables.ContainsKey(name.StrValue)) {
						PushStack(new Value(ValueType.NullVal));
					} else {
						var var = Variables[name.StrValue];

						PushStack(var);
					}
				} break;
				case OpCode.FetchField: {
					var fieldName = PopStack();
					var currentObject = PopStack();

					var objectValue = currentObject.ObjectValue.GetVariable(fieldName.StrValue);
                        
					PushStack(objectValue);
				} break;
				case OpCode.On: {
					var block = PopStack();
					var eventName = PopStack();
					var obj = PopStack();
                        
					if (!block.Is(ValueType.Block)) {
						return RaiseError("expected the on binding to be a code block");
					}

					if (!obj.Is(ValueType.Object)) {
						return RaiseError($"it is not an object you are trying to bind the event to");
					}
                        
					obj.ObjectValue.AddEvent(eventName.StrValue, block.BlockValue);
				} break;
				case OpCode.Run: {
					throw new NotImplementedException();
				} break;
				case OpCode.Gt:
				case OpCode.Lt:
				case OpCode.Gte:
				case OpCode.Lte:
				case OpCode.Mul:
				case OpCode.Div:
				case OpCode.Plus:
				case OpCode.Minus: {
					var second = PopStack();
					var first = PopStack();

					if (first.Is(ValueType.NullVal)) {
						first.Type = ValueType.Number;
						first.NumValue = 0;
					}

					if (second.Is(ValueType.NullVal)) {
						second.Type = ValueType.Number;
						second.NumValue = 0;
					}

					switch (nextInstr.Op) {
						case OpCode.Plus: {
							if (first.Is(ValueType.String) || second.Is(ValueType.String)) {
								string newStr = "";
								newStr += first.ForceToString();
								newStr += second.ForceToString();

								PushStack(new Value(newStr));
							} else if (first.Is(ValueType.Number) && second.Is(ValueType.Number)) {
								PushStack(new Value(first.NumValue + second.NumValue));
							} else {
								return RaiseError("can only add numbers together or values to strings");
							}
						} break;
						case OpCode.Minus: {
							if (first.Is(ValueType.Number) && second.Is(ValueType.Number)) {
								PushStack(new Value(first.NumValue - second.NumValue));
							} else {
								return RaiseError("can only minus numbers");
							}
						} break;
						case OpCode.Mul: {
							if (first.Is(ValueType.Number) && second.Is(ValueType.Number)) {
								PushStack(new Value(first.NumValue * second.NumValue));
							} else {
								return RaiseError("can only multiply numbers");
							}
						} break;
						case OpCode.Div: {
							if (first.Is(ValueType.Number) && second.Is(ValueType.Number)) {
								PushStack(new Value(first.NumValue / second.NumValue));
							} else {
								return RaiseError("can only divide numbers");
							}
						} break;
						case OpCode.Gt: {
							if(first.Is(ValueType.Number) && second.Is(ValueType.Number)) {
								PushStack(new Value(first.NumValue > second.NumValue));
							} else {
								return RaiseError("can only compare numbers");
							}
						} break;
						case OpCode.Lt: {
							if(first.Is(ValueType.Number) && second.Is(ValueType.Number)) {
								PushStack(new Value(first.NumValue < second.NumValue));
							} else {
								return RaiseError("can only compare numbers");
							}
						} break;
						case OpCode.Gte: {
							if(first.Is(ValueType.Number) && second.Is(ValueType.Number)) {
								PushStack(new Value(first.NumValue >= second.NumValue));
							} else {
								return RaiseError("can only compare numbers");
							}
						} break;
						case OpCode.Lte: {
							if(first.Type == ValueType.Number && second.Type == ValueType.Number) {
								PushStack(new Value(first.NumValue <= second.NumValue));
							} else {
								return RaiseError("can only compare numbers");
							}
						} break;
					}
				} break;
				case OpCode.Eq:
				case OpCode.Neq:
				case OpCode.Band:
				case OpCode.Bor: {
					var second = PopStack();
					var first = PopStack();

					switch(nextInstr.Op) {
						case OpCode.Eq: {
							PushStack(new Value(first.ForceToString() == second.ForceToString()));
						} break;
						case OpCode.Neq: {
							PushStack(new Value(first.ForceToString() != second.ForceToString()));
						} break;
						case OpCode.Band: {
							if(first.Is(ValueType.Boolean) && second.Is(ValueType.Boolean)) {
								PushStack(new Value(first.BoolValue && second.BoolValue));
							} else {
								return RaiseError("can only compare numbers");
							}
						} break;
						case OpCode.Bor: {
							if(first.Is(ValueType.Boolean) && second.Is(ValueType.Boolean)) {
								PushStack(new Value(first.BoolValue || second.BoolValue));
							} else {
								return RaiseError("can only compare numbers");
							}
						} break;
					}
				} break;
				case OpCode.Assign: {
					var val = PopStack();
					var name = PopStack();

					if(Variables.ContainsKey(name.StrValue) && Variables[name.StrValue].IsConst) {
						return RaiseError("cannot assign to a constant");
					}

					if (GlobalVariables.Contains(name.StrValue)) {
						val.IsGlobal = true;
					}
                        
					SetVariable(name.StrValue, val);
				} break;
				case OpCode.AssignField: {
					var val = PopStack();
					var name = PopStack();
					var objVal = (firstArg == 0) ? PopStack() : GetStackVal();

					var obj = objVal.ObjectValue;

					if(obj.Variables.ContainsKey(name.StrValue) && obj.Variables[name.StrValue].IsConst) {
						return RaiseError("cannot assign to a constant");
					}

					obj.SetVariable(name.StrValue, val);
				} break;
				case OpCode.Call: {
					var funcVal = PopStack(-firstArg);
					var shouldReturn = nextInstr.Args[1];

					if(!funcVal.Is(ValueType.NativeFunc)) {
						return RaiseError("cannot call a non function");
					}

					List<Value> args = new();
					for(int s = firstArg - 1; s >= 0; s--) {
						args.Add(PopStack(-s));
					}

					var stackSize = Stack.Count;

					var retVal = funcVal.FuncValue(this, args);
                        
					for (int c = 0; c < Stack.Count - stackSize; c++) {
						PopStack();
					}

					if (retVal != null) {
						PushStack(retVal.Value);
					}
				} break;
				case OpCode.Czjmp: {
					var success = GetStackVal();

					if(!success.BoolValue) {
						CurrentFrame().Ip += firstArg;
					}
				} break;
				case OpCode.Cjmp: {
					var success = GetStackVal();

					if(success.BoolValue) {
						CurrentFrame().Ip += firstArg;
					}
				} break;
				case OpCode.Jmp: {
					CurrentFrame().Ip += firstArg;
				} break;
				case OpCode.PushTrue:
				case OpCode.PushFalse: {
					Stack.Add(new Value(nextInstr.Op == OpCode.PushTrue));
				} break;
				case OpCode.Debug: {
					Console.WriteLine("DEBUG: {}", Compunit.Consts[firstArg].StrValue);
				} break;
				case OpCode.PushNumber: {
					PushStack(new Value(firstArg));  
				} break;
				case OpCode.Ret: {
					PopFrame();
	                    
					if(QueuedBlocks.Count > 0) {
						NextQueuedBlock();
					} else if(Frames.Count == 0) {
						State = VMState.Completed;
					}
				} break;
				case OpCode.Global: {
					for (int c = 0; c < firstArg; c++) {
						var ident = PopStack();
						if (!GlobalVariables.Contains(ident.StrValue)) {
							GlobalVariables.Add(ident.StrValue);
						}
					}
				} break;
				case OpCode.PushNewObject: {
					var objVal = new Value(new ScriptObject());
					addAnnotationToValue(objVal);
					PushStack(objVal);
				} break;
				case OpCode.Annotation: {
					var name = Compunit.Consts[firstArg];
                        
					NextAnnotations.Add(new ValueAnnotation(name.StrValue));
				} break;
				default: {
					throw new Exception("opcode is not handled!");
				}
			}
		}

		return State;
	}

}