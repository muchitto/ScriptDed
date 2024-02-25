using System;
using System.Collections.Generic;

namespace ScriptDed; 

public class CompUnit {
	public string Name = "";
	public string Filename = "";
	public List<Value> Consts = new();
	public List<Block> Blocks = new();

	public List<string> VariablesUsed = new();
	public List<string> VariablesAssigned = new();
	public List<string> GlobalVariables = new();
	public List<string> AddedEventsToObjectNamed = new();

	public void PrintDebugOutCompUnit() {
		Console.Write(DebugCompUnitData());
	}

	public string DebugCompUnitData() {
		var outStr = "";
		outStr += $"Module {Name}\n";
	        
		outStr += "consts:\n\n";

		{
			int i = 0;
			foreach (var c in Consts)
			{
				if (c.Type == ValueType.String)
				{
					outStr += i + ") " + c.StrValue + "\n";
				}
				else if (c.Type == ValueType.Number)
				{
					outStr += i + ") " + c.NumValue + "\n";
				}
				i++;
			}
		}

		outStr += "\nblocks:\n";

		for (var b = 0; b < Blocks.Count; b++) {
			outStr += "\n===BLOCK " + b + "===\n";
			outStr += "opcodes:\n";

			var block = Blocks[b];

			for (var i = 0; i < block.Instructions.Count; i++) {
				var instr = block.Instructions[i];

				if(instr.Op == OpCode.Debug) {
					var cnst = Consts[instr.Args[0]];
					outStr += "=== DEBUG: " + cnst.ForceToString() + " ===\n";
					continue;
				}

				var opcodeStr = instr.Op.ToString().ToUpper();

				if (instr.Op == OpCode.Const) {
					var cnst = Consts[instr.Args[0]];
					outStr += $"{i}) {opcodeStr} {instr.Args[0]} ({cnst.ForceToString()})\n";
				} else {
					outStr += $"{i}) {opcodeStr} {instr.Args[0]} {instr.Args[1]} {instr.Args[2]}\n";
				}
			}
		}

		return outStr;
	}
}