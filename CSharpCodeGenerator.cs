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
			var ctx = new CodeGenerationContext ();
			root.GenerateCode (ctx, writer);
		}
	}

	public partial class CodeGenerationContext
	{
		public TextWriter CurrentClassWriter { get; set; }
		public PropertySetter CurrentPropertySetter { get; set; }
		public bool InForHeadings { get; set; }

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
			// FIXME: cover all C# keywords.
			switch (name) {
			case "int":
				return "@int";
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
		
		public void WriteHeaders (MemberHeaders headers, TextWriter writer)
		{
			if (headers == null) // Constructor has no access modifier
				return;
			foreach (var header in headers) {
				switch (header) {
				case "Identifier": // first of all, the actual string value should be the label. Though, C# doesn't support namespace-specific name scope. So we write "public" for this instead.
					writer.Write ("public/*it is originally namespace-scoped*/ ");
					break;
				case "dynamic": // it is not supported in C# as an access modifier
					continue;
				default:
					writer.Write ("{0} ", header);
					break;
				}
			}
		}
	}

	public partial class CompileUnit
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			foreach (var item in Items)
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
			writer.WriteLine ("namespace {0}", Name);
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

			writer.WriteLine ("// class {0}", Name);
			foreach (var ev in Events)
				ev.GenerateCode (ctx, writer);
			ctx.WriteHeaders (Headers, writer);
			writer.WriteLine ("class {0}{1}{2}", Name, BaseClassName != null ? " : " : null, BaseClassName);
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
				writer.Write (member.Value);
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
			string name = Type.Raw;
			if (name.EndsWith (".*", StringComparison.Ordinal))
				writer.WriteLine ("using {0};", name.Substring (0, name.Length - 2));
			else
				writer.WriteLine ("using {0} = {1};", name.Substring (name.LastIndexOf ('.') + 1), name);
		}
	}

	public partial class NamespaceDeclaration : IPackageContent, INamespaceOrClass
	{
		public void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.WriteLine ("using {0};", Name);
		}
	}

	public partial class FieldDeclaration : ClassMemberBase
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			ctx.WriteHeaders (Headers, writer);
			foreach (var ntv in NameTypeValues) {
				// because AS3 variable types could differ within a line (unlike C#), they have to be declared in split form (or I have to do something more complicated.)
				if (ntv.Type != null)
					writer.Write (ntv.Type.ToCSharp ());
				else
					writer.Write ("dynamic");
				writer.Write (' ');
				writer.Write (ctx.SafeName (ntv.Name));
				if (ntv.Value != null) {
					writer.Write (" = ");
					ntv.Value.GenerateCode (ctx, writer);
				}
				writer.WriteLine (';');
			}
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
			ctx.WriteHeaders (Headers, writer);
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
			if (returnVoid)
				writer.Write ("void");
			else if (ReturnTypeName != null)
				writer.Write (ReturnTypeName.ToCSharp ());
			else if (!(this is Constructor))
				writer.Write ("/* no type? */");
			writer.Write (' ');
			writer.Write (namePrefix);
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
			writer.WriteLine (')');
			Body.GenerateCode (ctx, writer);
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
			Target.GenerateCode (ctx, writer);
			writer.Write (" {0}= ", ctx.ToCSharpCode (Operator));
			Value.GenerateCode (ctx, writer);
			if (!ctx.InForHeadings)
				writer.WriteLine (';');
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
	

	public partial class ForInStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			// probably the only difference from for-each is that it can declare local variable.
			writer.Write ("foreach (");
			Iterator.GenerateCode (ctx, writer);
			writer.Write (" in ");
			Target.GenerateCode (ctx, writer);
			writer.WriteLine (")");
			Body.GenerateCode (ctx, writer);
		}
	}

	public partial class ForEachStatement : Statement
	{
		public override void GenerateCode (CodeGenerationContext ctx, TextWriter writer)
		{
			writer.Write ("foreach (");
			Iterator.GenerateCode (ctx, writer);
			writer.Write (" in ");
			Target.GenerateCode (ctx, writer);
			writer.WriteLine (")");
			Body.GenerateCode (ctx, writer);
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
			bool gen = AccessType == MemberAccessType.GenericSubtype;
			if (Target != null) {
				Target.GenerateCode (ctx, writer);
				if (gen)
					writer.Write ("<" + GenericSubtype + ">");
				else
					writer.Write ("." + ctx.SafeName (Member));
			}
			else if (!gen)
				writer.Write (ctx.GetActualName (Member));
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
			writer.Write ("new object [] {");
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
			writer.Write ("var ");
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
			Left.GenerateCode (ctx, writer);
			writer.Write (' ');
			writer.Write (Operator);
			writer.Write (' ');
			Right.GenerateCode (ctx, writer);
			if (!ctx.InForHeadings)
				writer.WriteLine (';');
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

			int idx;
			while ((idx = s.IndexOf (".<")) >= 0)
				s = s.Substring (0, idx) + "<" + s.Substring (idx + 2);
			return s;
		}
	}
}
