asparser.exe : ActionScriptParser.dll
	gmcs -r:ActionScriptParser.dll -r:Irony.dll Driver.cs -debug

ActionScriptParser.dll : ActionScriptParser.cs ActionScriptAstBuilder.cs
	gmcs -t:library ActionScriptParser.cs ActionScriptAstBuilder.cs -debug -r:Irony.dll

clean:
	rm -rf ActionScriptParser.dll Driver.exe
