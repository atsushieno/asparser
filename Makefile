asparser.exe : ActionScriptParser.dll
	dmcs -r:ActionScriptParser.dll -r:Irony.dll Driver.cs -debug

ActionScriptParser.dll : ActionScriptParser.cs ActionScriptAstBuilder.cs
	dmcs -t:library ActionScriptParser.cs ActionScriptAstBuilder.cs -debug -r:Irony.dll
