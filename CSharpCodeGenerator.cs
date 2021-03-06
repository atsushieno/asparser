using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Irony.Parsing;

using MemberHeader = System.String;
using MemberHeaders = System.Collections.Generic.List<string>;
using Identifier = System.String;
using QualifiedReference = System.String;
using PackageContents = System.Collections.Generic.List<FreeActionScript.IPackageContent>;
using NamespaceUses = System.Collections.Generic.List<FreeActionScript.NamespaceUse>;
using EventDeclarations = System.Collections.Generic.List<FreeActionScript.EventDeclaration>;
using ClassMembers = System.Collections.Generic.List<FreeActionScript.IClassMember>;
using FunctionCallArguments = System.Collections.Generic.List<FreeActionScript.Expression>;
using Statements = System.Collections.Generic.List<FreeActionScript.Statement>;
using SwitchBlocks = System.Collections.Generic.List<FreeActionScript.SwitchBlock>;
using ForIterators = System.Collections.Generic.List<FreeActionScript.IForIterator>;
using ForAssignStatements = System.Collections.Generic.List<FreeActionScript.AssignmentExpressionStatement>;
using CatchBlocks = System.Collections.Generic.List<FreeActionScript.CatchBlock>;
using ArgumentDeclarations = System.Collections.Generic.List<FreeActionScript.ArgumentDeclaration>;
using NameTypeValues = System.Collections.Generic.List<FreeActionScript.NameTypeValue>;
using Expressions = System.Collections.Generic.List<FreeActionScript.Expression>;
using HashItems = System.Collections.Generic.List<FreeActionScript.HashItem>;

namespace FreeActionScript
{
	public class CSharpCodeGenerator
	{
		CompileUnit root;
		TextWriter writer;

		public CSharpCodeGenerator (CompileUnit root, TextWriter writer)
		{
			this.root = root;
			this.writer = writer;
		}

		public void GenerateCode ()
		{
			writer.WriteLine ("// This source is automatically generated");
			writer.WriteLine ("using System;");
			writer.WriteLine ("using System.Linq;");
			writer.WriteLine ("using Number = System.Double;");
			writer.WriteLine ("using Class = System.Type;");

			var ctx = new CodeGenerationContext ();
			root.GenerateCode (ctx, writer);
		}
	}

	public partial class CodeGenerationContext
	{
		public TextWriter CurrentClassWriter { get; set; }
		public ClassDeclaration CurrentType { get; set; } // probably needs to be modified to allow interfaces (so far they are ClassDeclaration too though)
		public PropertySetter CurrentPropertySetter { get; set; }
		public bool InForHeadings { get; set; }
		public bool DoNotEscape { get; set; }
		public TypeName KnownLValueType { get; set; }

		int anonidx;

		public int NextAnonymousMethodIndex ()
		{
			return anonidx++;
		}

		public Identifier GetActualName (Identifier name)
		{
			if (CurrentPropertySetter != null && CurrentPropertySetter.Definition.Name == name)
				return "value";
			else
				return SafeName (name);
		}

		public Identifier SafeName (Identifier name)
		{
			if (DoNotEscape)
				return name;

			// FIXME: cover all C# keywords.
			switch (name) {
			case "out":
				return "@out";
			case "int":
				return "@int";
			case "uint":
				return "@uint";
			case "event":
				return "@event";
			case "operator":
				return "@operator";
			}
			if (name.Length > 0 && name [0] == '$')
				return "__" + name.Substring (1);
			return name;
		}

		public string ToCSharpCode (AssignmentOperators oper)
		{
			switch (oper) {
			case AssignmentOperators.Plus: return "+";
			case AssignmentOperators.Minus: return "-";
			case AssignmentOperators.Multiply: return "*";
			case AssignmentOperators.Divide: return "/";
			case AssignmentOperators.Modulo: return "%";
			case AssignmentOperators.BitwiseAnd: return "&";
			case AssignmentOperators.BitwiseOr: return "|";
			case AssignmentOperators.ShiftLeft: return "<<";
			case AssignmentOperators.ShiftRight: return ">>";
			}
			throw new ArgumentException ();
		}
		
		public void WriteHeaders (MemberHeaders headers, TextWriter writer, bool autoFillVirtual, bool suppressStatic)
		{
			if (headers == null) // Constructor has no access modifier
				return;
			foreach (var header in headers) {
				switch (header) {
				case "Identifier": // first of all, the actual string value should be the label. Though, C# doesn't support namespace-specific name scope. So we write "public" for this instead.
					writer.Write ("public/*it is originally namespace-scoped*/ ");
					break;
				case "final":
					writer.Write ("sealed ");
					break;
				case "dynamic": // it is not supported in C# as an access modifier
					continue;
				case "static":
					if (suppressStatic)
					continue;
					goto default;
				default:
					writer.Write ("{0} ", header);
					break;
				}
			}
			if (autoFillVirtual && !headers.Any (h => Array.IndexOf (virtual_suppressor, h) >= 0))
				writer.Write ("virtual ");
		}
		
		static readonly string [] virtual_suppressor = {"override", "private", "static"};
	}

	public partial class CompileUnit
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			foreach (var item in Items)
				if (item is Import)
					item.GenerateCode (ctx, writer);
			foreach (var item in Items)
				if (!(item is Import))
					item.GenerateCode (ctx, writer);
		}
	}

	public partial interface ICompileUnitItem
	{
		void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}

	public partial class PackageDeclaration : ICompileUnitItem
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.WriteLine ("namespace {0}", Name.ToCSharp ());
			writer.WriteLine ("{");
			foreach (var item in Items)
				item.GenerateCode (ctx, writer);
			writer.WriteLine ("}");
		}
	}

	public partial interface IPackageContent
	{
		void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}

	public partial class ClassDeclaration : ICompileUnitItem, IPackageContent, INamespaceOrClass
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			var parentClassWriter = ctx.CurrentClassWriter;
			var sw = new StringWriter ();
			ctx.CurrentClassWriter = sw;
			var parentType = ctx.CurrentType;
			ctx.CurrentType = this;

			writer.WriteLine ("// class {0}", Name);
			foreach (var ev in Events)
				ev.GenerateCode (ctx, writer);
			ctx.WriteHeaders (Headers, writer, false, false);
			writer.WriteLine ("partial {3} {0}{1}{2}", Name, BaseClassName != null ? " : " : /*" : global::Object"*/String.Empty, BaseClassName, IsInterface ? "interface" : "class");
			writer.WriteLine ("{");
			foreach (var nsuse in NamespaceUses) {
				writer.WriteLine ("// FIXME: using directive inside class declaration is not allowed in C#");
				writer.WriteLine ("// using {0};", nsuse.Name);
			}
			foreach (var item in Members)
				item.GenerateCode (ctx, writer);

			// output temporarily saved members (such as local functions)
			writer.Write (sw.ToString ());

			writer.WriteLine ("}");
			writer.WriteLine ("// end of class {0}", Name);

			ctx.CurrentType = parentType;
			ctx.CurrentClassWriter = parentClassWriter;
		}
	}

	public partial class EventDeclaration
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("[");
			writer.Write (Name.ToCSharp ());
			writer.Write ("(");
			foreach (var member in Members) {
				var tail = Members.Last () == member ? "" : ", ";
				writer.Write (ctx.SafeName (member.Name));
				writer.Write (" = ");
				member.Value.GenerateCode (ctx, writer);
				writer.Write (tail);
			}
			writer.WriteLine (")]");
		}
	}

	public partial interface IClassMember
	{
		void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}

	public abstract partial class ClassMemberBase : IClassMember
	{
		public abstract void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}

	public partial class Import : ICompileUnitItem, IPackageContent
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			string name = Type.ToCSharp ();

			// package functions cannot be imported. So disable them (as comment).
			switch (name) {
			case "flash.utils.getTimer":
				writer.Write ("// ");
				break;
			}

			// FIXME: this is sort of hack, but it is not really definite way to determine if the import is for a namespace or a type. So, basically I treat such ones that 1) if it ends with .* or 2) if the final identifier after '.' begins with Uppercase, as a namespace.
			bool isNS = name.EndsWith (".*", StringComparison.Ordinal);
			if (isNS)
				name = name.Substring (0, name.Length - 2);
			int idx = name.LastIndexOf ('.');
			if (idx > 0 && idx + 1 < name.Length && !Char.IsUpper (name [idx + 1]))
				isNS = true;
			if (isNS)
				writer.WriteLine ("using {0};", name);
			else
				writer.WriteLine ("using {0} = {1};", name.Substring (name.LastIndexOf ('.') + 1), Type.ToCSharp ());
		}
	}

	public partial class NamespaceDeclaration : IPackageContent, INamespaceOrClass
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			// There is no corresponding concept in C# for AS3's namespace inside package - I just output namespace inside namespace to suppress error messages.
			writer.WriteLine ("namespace {0} {{}}", Name.ToCSharp ());
		}
	}

	public partial class FieldDeclaration : ClassMemberBase
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			ctx.WriteHeaders (Headers, writer, false, this is ConstantDeclaration);
			var bak = ctx.KnownLValueType;
			foreach (var ntv in NameTypeValues) {
				// because AS3 variable types could differ within a line (unlike C#), they have to be declared in split form (or I have to do something more complicated.)
				if (this is ConstantDeclaration)
					writer.Write ("const ");
				if (ntv.Type != null)
					writer.Write (ntv.Type.ToCSharp ());
				else if (ntv.Value != null)
					writer.Write ("var");
				else
					writer.Write ("dynamic");
				ctx.KnownLValueType = ntv.Type;
				writer.Write (' ');
				writer.Write (ctx.SafeName (ntv.Name));
				if (ntv.Value != null) {
					writer.Write (" = ");
					ntv.Value.GenerateCode (ctx, writer);
				}
				writer.WriteLine (';');
				ctx.KnownLValueType = null;
			}
			ctx.KnownLValueType = bak;
		}
	}

	public partial class GeneralFunction : ClassMemberBase
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			OnGenerateCode (ctx, writer, false, null);
		}

		internal void OnGenerateCode (CodeGenerationContext ctx, TextWriter writer, bool returnVoid, string namePrefix)
		{
			// looks like only constructors have no return type.
			// Body is checked to distinguish class and interface.
			bool isInterface = Definition.Body == null;
			ctx.WriteHeaders (Headers, writer, Definition.ReturnTypeName != null && !isInterface, false);
			Definition.OnGenerateCode (ctx, writer, returnVoid, namePrefix);
			writer.WriteLine ();
		}
	}

	public partial class FunctionDefinition
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			OnGenerateCode (ctx, writer, false, null);
		}
		
		internal void OnGenerateCode (CodeGenerationContext ctx, TextWriter writer, bool returnVoid, string namePrefix)
		{
			bool isConstructor = ctx.CurrentType != null && ctx.CurrentType.Name == Name;
			if (returnVoid)
				writer.Write ("void");
			else if (!isConstructor && ReturnTypeName != null)
				writer.Write (ReturnTypeName.ToCSharp ());
			else if (!isConstructor)
				writer.Write ("/* no type? */");
			writer.Write (' ');
			writer.Write (namePrefix);
			if (Name == "getTimer")
				writer.Write ("GlobalContext.getTimer");
			else if (Name == "toString")
				writer.Write ("ToString");
			else
				writer.Write (ctx.SafeName (Name));
			writer.Write (" (");
			foreach (var arg in Arguments) {
				var tail = Arguments.Last () == arg ? "" : ", ";
				if (arg.IsVarArg)
					writer.Write ("params object [] {0}", arg.Name);
				else {
					writer.Write (arg.Type.ToCSharp ());
					writer.Write (' ');
					writer.Write (ctx.SafeName (arg.Name));
					if (arg.DefaultValue != null) {
						writer.Write (" = ");
						arg.DefaultValue.GenerateCode (ctx, writer);
					}
				}
				writer.Write (tail);
			}
			writer.Write (')');
			if (Body == null) // interface
				writer.WriteLine (';');
			else {
				writer.WriteLine ();
				Body.GenerateCode (ctx, writer);
			}
		}
	}

	public partial class PropertyGetter : GeneralFunction
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			// FIXME: implement, merging with setter (needs some class context)
			base.OnGenerateCode (ctx, writer, false, "get_");
		}
	}

	public partial class PropertySetter : GeneralFunction
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			// FIXME: implement, merging with setter (needs some class context)

			// Property syntax in AS3 is weird. 
			// Parameter name doesn't make any sense.
			// The property name is used like "value" in C#.
			// Hence, we have to save current property setter
			// and when resolving name reference we have to
			// "rename" it.
			var curProp = ctx.CurrentPropertySetter;
			ctx.CurrentPropertySetter = this;
			base.OnGenerateCode (ctx, writer, true, "set_");
			ctx.CurrentPropertySetter = curProp;
		}
	}

	public abstract partial class Statement
	{
		public abstract void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}

	public partial class ExpressionStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Expression.GenerateCode (ctx, writer);
			if (!ctx.InForHeadings)
				writer.WriteLine (';');
		}
	}

	public partial class AssignmentStatement : Statement, IForIterator
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			var bak = ctx.KnownLValueType;
			Target.GenerateCode (ctx, writer);
			ctx.KnownLValueType = Target.Type;
			writer.Write (" {0}= ", ctx.ToCSharpCode (Operator));
			Value.GenerateCode (ctx, writer);
			if (!ctx.InForHeadings)
				writer.WriteLine (';');
			ctx.KnownLValueType = bak;
		}
	}

	public partial class ReturnStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (Value == null)
				writer.Write ("return;");
			else {
				writer.Write ("return ");
				Value.GenerateCode (ctx, writer);
				writer.Write (';');
			}
			writer.WriteLine ();
		}
	}

	public partial class IfStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("if (");
			Condition.GenerateCode (ctx, writer);
			writer.WriteLine (")");
			TrueStatement.GenerateCode (ctx, writer);
			if (FalseStatement != null) {
				writer.WriteLine ("else");
				FalseStatement.GenerateCode (ctx, writer);
			}
		}
	}

	public partial class SwitchStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("switch (");
			Condition.GenerateCode (ctx, writer);
			writer.WriteLine (") {");
			foreach (var caseBlock in CaseBlocks)
				caseBlock.GenerateCode (ctx, writer);
			writer.WriteLine ("}");
		}
	}

	public partial class SwitchBlock
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (Label == Default)
				writer.WriteLine ("default:");
			else {
				writer.Write ("case ");
				if (Label is Literal)
					((Literal) Label).GenerateCode (ctx, writer);
				else
					writer.Write ((Identifier) Label);
				writer.WriteLine (':');
			}
			foreach (var stmt in Statements)
				stmt.GenerateCode (ctx, writer);
		}
	}

	public partial class WhileStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("while (");
			Condition.GenerateCode (ctx, writer);
			writer.WriteLine (")");
			Body.GenerateCode (ctx, writer);
		}
	}

	public partial class DoWhileStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("do");
			Body.GenerateCode (ctx, writer);
			writer.WriteLine ("while (");
			Condition.GenerateCode (ctx, writer);
			writer.WriteLine (");");
		}
	}

	public partial class ForStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			ctx.InForHeadings = true;
			writer.Write ("for (");
			Initializers.GenerateCode (ctx, writer);
			writer.Write ("; ");
			Condition.GenerateCode (ctx, writer);
			writer.Write ("; ");
			foreach (var iter in Iterators) {
				var tail = Iterators.Last () == iter ? "" : ", ";
				iter.GenerateCode (ctx, writer);
				writer.Write (tail);
			}
			ctx.InForHeadings = false;
			writer.WriteLine (')');
			Body.GenerateCode (ctx, writer);
		}
	}

	public partial interface IForIterator
	{
		void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}
	

	public abstract partial class ForEachInStatementBase : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			// probably the only difference from for-each is that it can declare local variable.
			writer.Write ("foreach (var ___");
			writer.Write (" in ");
			Target.GenerateCode (ctx, writer);
			writer.WriteLine (") {");
			Iterator.GenerateCode (ctx, writer);
			writer.WriteLine (" = ___;");
			Body.GenerateCode (ctx, writer);
			writer.WriteLine ("}");
		}
	}

	public partial class ForInitializers
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (LocalVariables != null)
				LocalVariables.GenerateCode (ctx, writer);
			else {
				foreach (var axs in AssignStatements) {
					var tail = AssignStatements.Last () == axs ? "" : ", ";
					axs.GenerateCode (ctx, writer);
					writer.Write (tail);
				}
			}
		}
	}

	public partial class ForEachIterator
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (LocalVariableType != null) {
				writer.Write (LocalVariableType.ToCSharp ());
				writer.Write (' ');
			}
			writer.Write (ctx.SafeName (Name));
		}
	}

	public partial class BreakStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.WriteLine ("break;");
		}
	}
	
	public partial class ContinueStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.WriteLine ("continue;");
		}
	}

	public partial class BlockStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.WriteLine ("{");
			foreach (var stmt in Statements)
				stmt.GenerateCode (ctx, writer);
			writer.WriteLine ("}");
		}
	}

	public partial class ThrowStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("throw ");
			Target.GenerateCode (ctx, writer);
			writer.WriteLine (';');
		}
	}

	public partial class TryStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("try ");
			TryBlock.GenerateCode (ctx, writer);
			foreach (var cb in CatchBlocks)
				cb.GenerateCode (ctx, writer);
			if (FinallyBlock != null) {
				writer.Write (" finally ");
				FinallyBlock.GenerateCode (ctx, writer);
			}
		}
	}

	public partial class CatchBlock
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("catch (");
			writer.Write (NameAndType.Type.ToCSharp ());
			writer.Write (' ');
			writer.Write (ctx.SafeName (NameAndType.Name));
			writer.Write (')');
			Block.GenerateCode (ctx, writer);
		}
	}

	public partial class DeleteStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			// if (xxx is IDisposable) ((IDisposable) xxx).Dispose ();
			writer.Write ("if (");
			Target.GenerateCode (ctx, writer);
			writer.WriteLine (" is System.IDisposable)");
			writer.WriteLine ("((IDisposable) ");
			Target.GenerateCode (ctx, writer);
			writer.WriteLine (").Dispose ();");
		}
	}

	public abstract partial class Expression
	{
		public abstract void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}

	public partial class ConditionalExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Condition.GenerateCode (ctx, writer);
			writer.Write (" ? ");
			TrueValue.GenerateCode (ctx, writer);
			writer.Write (" : ");
			FalseValue.GenerateCode (ctx, writer);
		}
	}
	
	public partial class BinaryExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Left.GenerateCode (ctx, writer);
			writer.Write (' ');
			switch (Operator) {
			case ">>>": writer.Write (">> /* >>> */"); break;
			case "<<<": writer.Write ("<< /* <<< */"); break;
			case "===": writer.Write ("== /* === */"); break;
			case "!==": writer.Write ("!= /* === */"); break;
			default: writer.Write (Operator); break;
			}
			writer.Write (' ');
			Right.GenerateCode (ctx, writer);
		}
	}

	public partial class UnaryExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write (Operator);
			Primary.GenerateCode (ctx, writer);
		}
	}

	public partial class IncrementDecrementExpression : UnaryExpression, ICalcAssignStatement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (IsPostfix) {
				Primary.GenerateCode (ctx, writer);
				writer.Write (Operator);
			} else {
				writer.Write (Operator);
				Primary.GenerateCode (ctx, writer);
			}
		}
	}
	
	public partial interface ILeftValue // things that can be lvalue
	{
		void GenerateCode (CodeGenerationContext ctx, TextWriter writer);
	}
	
	public partial class ArrayAccessExpression : Expression, ILeftValue
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Array.GenerateCode (ctx, writer);
			writer.Write ('[');
			Index.GenerateCode (ctx, writer);
			writer.Write (']');
		}
	}
	
	public partial class ArrayInExpression : Expression, ILeftValue
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("Array.IndexOf (");
			Array.GenerateCode (ctx, writer);
			writer.Write (", ");
			Threshold.GenerateCode (ctx, writer);
			writer.Write (") >= 0");
		}
	}

	public partial class ParenthesizedExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ('(');
			Content.GenerateCode (ctx, writer);
			writer.Write (')');
		}
	}

	public partial class FunctionCallExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Target.GenerateCode (ctx, writer);
			writer.Write ('(');
			foreach (var expr in Arguments) {
				var tail = Arguments.Last () == expr ? "" : ", ";
				expr.GenerateCode (ctx, writer);
				writer.Write (tail);
			}
			writer.Write (')');
		}
	}

	public partial class CastAsExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Primary.GenerateCode (ctx, writer);
			writer.Write (" as ");
			writer.Write (Type.ToCSharp ());
		}
	}

	public partial class IsExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Primary.GenerateCode (ctx, writer);
			writer.Write (" is ");
			writer.Write (Type.ToCSharp ());
		}
	}

	public partial class MemberReferenceExpression : Expression, ILeftValue
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Target.GenerateCode (ctx, writer);
		}
	}

	public partial class MemberReference
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			switch (Member != null ? Member.ToString () : null) {
			case "MIN_VALUE":
				ctx.DoNotEscape = true;
				Target.GenerateCode (ctx, writer);
				ctx.DoNotEscape = false;
				writer.Write ('.');
				writer.Write ("MinValue");
				break;
			case "MAX_VALUE":
				ctx.DoNotEscape = true;
				Target.GenerateCode (ctx, writer);
				ctx.DoNotEscape = false;
				writer.Write ('.');
				writer.Write ("MaxValue");
				break;
			default:
				bool gen = AccessType == MemberAccessType.GenericSubtype;
				if (Target != null) {
					if (gen)
						writer.Write ("new ");
					Target.GenerateCode (ctx, writer);
					if (gen)
						writer.Write ("<" + GenericSubtype + ">");
					else
						writer.Write ("." + ctx.SafeName (Member));
				}
				else if (!gen)
					writer.Write (ctx.GetActualName (Member));
				break;
			}
		}
	}

	public partial class NewObjectExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("new ");
			writer.Write (Name.ToCSharp ());
			writer.Write ('(');
			foreach (var expr in Arguments) {
				var tail = Arguments.Last () == expr ? "" : ", ";
				expr.GenerateCode (ctx, writer);
				writer.Write (tail);
			}
			writer.Write (')');
		}
	}

	public partial class LiteralArrayExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			var type = ctx.KnownLValueType != null ? ctx.KnownLValueType.GenericSubtype : null;

			writer.Write ("new {0} [] {{", type);
			foreach (var expr in Values) {
				var tail = Values.Last () == expr ? "" : ", ";
				expr.GenerateCode (ctx, writer);
				writer.Write (tail);
			}
			writer.Write ('}');
		}
	}

	public partial class LiteralHashExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("Util.CreateLiteralHash (new object [] {");
			foreach (var pair in Values) {
				var tail = Values.Last () == pair ? "" : ", ";
				if (pair.Key is Literal)
					((Literal) pair.Key).GenerateCode (ctx, writer);
				else
					writer.Write (ctx.SafeName ((Identifier) pair.Key));
				writer.Write (", ");
				pair.Value.GenerateCode (ctx, writer);
				writer.Write (tail);
			}
			writer.Write ("})");
		}
	}

	public partial class EmbeddedFunctionExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (Function.Name == null) {
				Function.Name = "__anonymous" + ctx.NextAnonymousMethodIndex ();
				Function.GenerateCode (ctx, ctx.CurrentClassWriter);
			}
			writer.WriteLine ("/* embedded function {0} is written as a class member.*/", Function.Name);
			writer.Write (Function.Name);
		}
	}

	public partial class LocalFunctionStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.WriteLine ("// local function {0} is written as a class member.", Function.Name);
			Function.GenerateCode (ctx, ctx.CurrentClassWriter);
		}
	}

	public partial class LocalVariableDeclarationStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (Pairs.Any (p => p.Value != null))
				writer.Write ("var ");
			else
				writer.Write ("dynamic ");

			foreach (var pair in Pairs) {
				var tail = Pairs.Last () == pair ? "" : ", ";
				writer.Write (ctx.SafeName (pair.Name));
				if (pair.Value != null) {
					writer.Write (" = ");
					pair.Value.GenerateCode (ctx, writer);
				}
				writer.Write (tail);
			}
			if (!ctx.InForHeadings)
				writer.WriteLine (';');
		}
	}

	public partial class CalcAssignStatement : Statement, ICalcAssignStatement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			switch (Operator) {
			case "<<<=":
				GenerateFunctionBasedExpr (ctx, writer, "RotateLeft");
				break;
			case ">>>=":
				GenerateFunctionBasedExpr (ctx, writer, "RotateRight");
				break;
			default:
				Left.GenerateCode (ctx, writer);
				writer.Write (' ');
				writer.Write (Operator);
				writer.Write (' ');
				Right.GenerateCode (ctx, writer);
				if (!ctx.InForHeadings)
					writer.WriteLine (';');
				break;
			}
		}

		void GenerateFunctionBasedExpr (CodeGenerationContext ctx, TextWriter writer, string name)
		{
			Left.GenerateCode (ctx, writer);
			writer.Write (" = ");
			writer.Write (name);
			writer.Write (" (");
			Left.GenerateCode (ctx, writer);
			writer.Write (", ");
			Right.GenerateCode (ctx, writer);
			writer.Write (");");
		}
	}

	public partial class AssignmentExpression : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			Left.GenerateCode (ctx, writer);
			writer.Write (" = ");
			Right.GenerateCode (ctx, writer);
		}
	}

	public partial class Literal : Expression
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			if (Value == null)
				writer.Write ("null");
			else if (Value is string) {
				string s = Value as string;
				if (s [0] == '\'') { // 'foo' -> "foo"
					writer.Write ('"');
					writer.Write (s.Substring (1, s.Length - 2));
					writer.Write ('"');
				}
				else
					writer.Write (s); // huh? why is Value double-quoted?
			}
			else if (Value is char)
				writer.Write ("\'" + Value + "\'");
			else if (Value is long || Value is ulong || Value is double || Value is decimal)
				writer.Write (Value.ToString ());
			else if (Value is RegexLiteral) {
				writer.Write ("new System.Text.RegularExpressions.Regex (@\"");
				writer.Write (((RegexLiteral) Value).Pattern);
				writer.Write ("\")");
			}
			else
				throw new NotImplementedException ();
		}
	}

	public partial class TypeName
	{
		public string ToCSharp ()
		{
			string s = Raw;

			if (s == "*")
				return "dynamic";

			var arr = s.Split ('.');
			for (int i = 0; i < arr.Length; i++) {
				// FIXME: support all C# keywords.
				if (arr [i] == "base")
					arr [i] = "_" + arr [i];
				else
					arr [i] = arr [i];
			}
			s = String.Join (".", arr);

			int idx;
			while ((idx = s.IndexOf (".<")) >= 0)
				s = s.Substring (0, idx) + "<" + s.Substring (idx + 2);
			return s;
		}
	}
}
