using System;
using System.Collections.Generic;
using System.Reflection;

namespace ScriptDed; 

public class ScriptObject : DataHolder {
	public ScriptObject () {

	}

	public List<Block> GetEvents(string eventName) {
		if(!HasEvent(eventName)) {
			return new List<Block>();
		}

		return Events[eventName];
	}

	public List<Block> GetVarEvents(string var, string eventName) {
		if(!HasVar(var)) {
			return new List<Block>();
		}

		if(Variables[var].Type != ValueType.Object) {
			return new List<Block>();
		}

		if(!Variables[var].ObjectValue.HasEvent(eventName)) {
			return new List<Block>();
		}

		return Variables[var].ObjectValue.Events[eventName];
	}
}