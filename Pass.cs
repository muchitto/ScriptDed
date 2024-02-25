namespace ScriptDed; 

public class Pass {
	public void RaiseError(string message, Position position) {
		throw new CompileError(message, position);
	}

	public void RaiseError(string message, Token token) {
		RaiseError(message, token.Position);
	}
}