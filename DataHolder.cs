using System.Collections.Generic;

namespace ScriptDed; 

public delegate Value GetVariableEvent(string name, Value variable);

public delegate bool SetVariableEvent(string name, Value value);
	
public class DataHolder {
	public Dictionary<string, Value> Variables = new();
	public Dictionary<string, List<Block>> Events = new();
        
	public event GetVariableEvent GetVariableEvents;
	public event SetVariableEvent SetVariableEvents;

	public void EnableEvent(string eventName) {
		if(!Events.ContainsKey(eventName)) {
			Events[eventName] = new List<Block>();
		}
	} 

	public void AddEvent(string eventName, Block block) {
		EnableEvent(eventName);

		if (Events[eventName].Contains(block)) {
			return;
		}

		Events[eventName].Add(block);
	}

	public Value GetVariable(string name) {
		if(!Variables.ContainsKey(name)) {
			var nullVal = new Value(ValueType.NullVal);
			GetVariableEvents?.Invoke(name, nullVal);
			return nullVal;
		}

		var variable = Variables[name];
            
		GetVariableEvents?.Invoke(name, variable);
            
		return variable;
	}

	public bool SetVariable(string name, Value value) {
		Variables[name] = value;

		SetVariableEvents?.Invoke(name, value);

		return true;
	}

	public virtual bool SetFunction(string name, NativeFunc func, bool isGlobal = false) {
		var val = new Value(func);
		val.IsConst = true;
		val.IsGlobal = true;
		return SetVariable(name, val);
	}

	public bool HasVar(string var) {
		return Variables.ContainsKey(var);
	}

	public bool HasEvent(string eventName) {
		return Events.ContainsKey(eventName);
	}
}