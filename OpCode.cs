namespace ScriptDed; 

public enum OpCode {
	Global,
	On,
	Ret,
	Const,
	Const2, // the const num + 255
	Fetch,
	FetchField,
	Assign,
	AssignField,
	Call,
	Run,
	PushNumber,
	PushBlock,
	PushTrue,
	PushFalse,
	PushNewObject,
	PushNull,
	Annotation,
	Jmp,
	Cjmp,
	Czjmp,

	Plus,
	Minus,
	Div,
	Mul,
	Eq,
	Neq,
	Gt,
	Lt,
	Lte,
	Gte,
	Band,
	Bor,

	Debug,
}

public class Instr {
	public OpCode Op;
	public byte[] Args = new byte[3];
}