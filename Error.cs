using System;

namespace ScriptDed; 

class Error : Exception {
	public string Message;

	public Error(string msg) : base(msg) {
		this.Message = msg;
	}
}

class CompileError : Error {
	public Position Position;

	public CompileError(string message, Position position) : base(message) {
		this.Position = position;
	}

	public CompileError(string message) : base(message) {
	}
}