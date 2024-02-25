namespace ScriptDed; 

public class Position {
	public string SourceCode = "";
	public string Filename = "";

	public int Row = 1;
	public int Col = 1;
	public int ColFrom = 0;
	public int ColTo = 0;

	public Position() {
	}

	public Position(string filename, string sourceCode) {
		this.Filename = filename;
		this.SourceCode = sourceCode;
	}
}