using System.Collections.Generic;

namespace ScriptDed; 

public enum CompilationOption {
	StaticTyping,
	ImplicitVarDecl,
}

public enum TypeInfoType {
	String,
	Number,
	Function,
	Object,
}

public class TypeInfo {
	public TypeInfoType Type;

}

public class Compilation {
	public List<string> Files = new();
	public List<CompilationOption> Options = new();
	public Dictionary<string, CompUnit> Compilations = new();
	public List<string> GlobalVariables = new();

	public Compilation() {

	}

	public void CompileAll() {
	}

	public void AddOptions(params CompilationOption[] options) {
		foreach (var option in options) {
			Options.Add(option);
		}
	}
}