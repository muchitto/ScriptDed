namespace ScriptDed; 

public class Ast {
	public Position Position;
	public List<AstAnnotation> Annotations = new();
}

public class AstBlock : Ast {
	public List<Ast> Children = new();
}

public class AstString : Ast {
	public string Value;

	public AstString(string value) {
		Value = value; 
	}
}

public class AstNumber : Ast {
	public double Number;

	public AstNumber (double number) {
		Number = number;
	}
}

public class AstIdentifier : Ast {
	public string Name;
	public AstIdentifier? SubField;

	public AstIdentifier(string name, AstIdentifier? subField = null) {
		Name = name;
		SubField = subField;
	}
}

public class AstAssign : Ast {
	public AstIdentifier Ident;
	public Ast Expr;
	public TokenType? Op = null;

	public AstAssign(AstIdentifier ident, Ast expr, TokenType op)  {
		Op = op;
		this.Ident = ident;
		this.Expr = expr;
	}
}

public class AstOn : Ast {
	public AstIdentifier Handle;
	public AstRun Run;

	public AstOn (AstIdentifier handle, AstRun run) {
		Handle = handle;
		Run = run;
	}
}

public class AstCall : Ast {
	public AstIdentifier Name = null;
	public List<Ast> Args = new();

	public AstCall(AstIdentifier? name = null) : base() {
		this.Name = name;
	}
}

public class AstExpr : Ast {
	public Ast Lhs;
	public Ast Rhs;
	public TokenType Op;
}

public class AstGlobal : Ast {
	public List<AstIdentifier> Identifiers = new();
}

public class AstRun : Ast {
	public Ast Run;
}

public class AstIf : Ast {
	public Ast? Expr;
	public AstIf NextIf;
	public AstBlock Block;
}

public class AstBool : Ast {
	public bool BoolVal;

	public AstBool(bool value) {
		BoolVal = value;
	}

	public AstBool(bool value, Position position) {
		BoolVal = value;
		Position = position;
	}
}

public class AstNull : Ast {
}

public class AstObject : Ast {
	public Dictionary<string, Ast> Fields = new();
}

public class AstAnnotation : Ast {
	public string Name = "";
}