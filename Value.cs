using System.Collections.Generic;

namespace ScriptDed; 

public enum ValueType {
	String,
	Number,
	Boolean,
	Block,
	Object,
	NullVal,
	NativeFunc,
}

public struct ValueAnnotation {
	public string Name;
        
	public ValueAnnotation(string name) {
		Name = name;
	}
}

public delegate Value? NativeFunc(VM vm, List<Value> args);

public struct Value {
	public ValueType Type;
	public bool IsConst;
	public bool IsGlobal;

	public string StrValue;
	public double NumValue;
	public bool BoolValue;
	public Block? BlockValue;
	public ScriptObject? ObjectValue;
	public NativeFunc? FuncValue;

	public List<ValueAnnotation> Annotations;

	public Value(ValueType type) {
		Type = type;
            
		IsConst = false;
		IsGlobal = false;
            
		StrValue = "";
		NumValue = 0;
		BoolValue = false;
		BlockValue = null;
		ObjectValue = null;
		FuncValue = null;
		Annotations = new();
	}

	public Value(NativeFunc func) : this(ValueType.NativeFunc) {
		FuncValue = func;
	}

	public Value(string strValue) : this(ValueType.String) {
		StrValue = strValue;
	}

	public Value(double numValue) : this(ValueType.Number) {
		NumValue = numValue;
	}

	public Value(bool boolValue) : this(ValueType.Boolean) {
		BoolValue = boolValue;
	}

	public Value(Block blockValue) : this(ValueType.Block) {
		BlockValue = blockValue;   
	}

	public Value(ScriptObject objectValue) : this(ValueType.Object) {
		ObjectValue = objectValue;
	}

	public bool HasAnnotation(string name) {
		foreach (var annotation in Annotations) {
			if (annotation.Name == name) {
				return true;
			}
		}

		return false;
	}
        
	public bool Is(ValueType type) {
		return Type == type;
	}

	public string ForceToString () {
		switch(Type) {
			case ValueType.Block:
				return "[block]";
			case ValueType.NativeFunc:
				return "[nativefunc]";
			case ValueType.Boolean:
				return BoolValue ? "true" : "false";
			case ValueType.NullVal:
				return "null";
			case ValueType.Number: {
				if(NumValue == (int) NumValue) {
					return "" + (int)NumValue;
				}

				return NumValue + "";
			}
			case ValueType.String:
				return StrValue;
		}

		return "";
	}
}