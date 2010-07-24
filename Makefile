DLL_SOURCES = \
	ActionScriptParser.cs \
	ActionScriptAstBuilder.cs \
	CSharpCodeGenerator.cs

asparser.exe : ActionScriptParser.dll
	gmcs -r:ActionScriptParser.dll -r:Irony.dll Driver.cs -debug

ActionScriptParser.dll : $(DLL_SOURCES)
	gmcs -t:library $(DLL_SOURCES) -debug -r:Irony.dll

clean:
	rm -rf ActionScriptParser.dll Driver.exe
