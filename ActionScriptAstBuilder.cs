using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Irony.Parsing;

using MemberHeader = System.String;
using MemberHeaders = System.Collections.Generic.List<string>;
using TypeName = System.String;
using Identifier = System.String;
using QualifiedReference = System.String;
using Literal = System.Object;
using PackageContents = System.Collections.Generic.List<FreeActionScript.IPackageContent>;
using NamespaceUses = System.Collections.Generic.List<FreeActionScript.NamespaceUse>;
using EventDeclarations = System.Collections.Generic.List<FreeActionScript.EventDeclaration>;
using EventDeclarationMembers = System.Collections.Generic.List<FreeActionScript.EventDeclarationMember>;
using ClassMembers = System.Collections.Generic.List<FreeActionScript.IClassMember>;
using FunctionCallArguments = System.Collections.Generic.List<FreeActionScript.Expression>;
using Statements = System.Collections.Generic.List<FreeActionScript.Statement>;
using SwitchBlocks = System.Collections.Generic.List<FreeActionScript.SwitchBlock>;
using ForIterators = System.Collections.Generic.List<FreeActionScript.IForIterator>;
using ForAssignStatements = System.Collections.Generic.List<FreeActionScript.AssignmentExpressionStatement>;
using TypedIdentifier = System.Collections.Generic.KeyValuePair<Identifier, TypeName>;
using ArgumentDeclarations = System.Collections.Generic.List<FreeActionScript.ArgumentDeclaration>;
using NameValuePairs = System.Collections.Generic.List<FreeActionScript.NameValuePair>;
using Expressions = System.Collections.Generic.List<FreeActionScript.Expression>;
using HashItems = System.Collections.Generic.List<FreeActionScript.HashItem>;

namespace FreeActionScript
{
	/*
	public class ActionScriptAstBuilder
	{
		public static ActionScriptAstNode Build (ParseTree ast)
		{
			return new ActionScriptAstConverter (ast).Build ();
		}

		ParseTree source;
		ActionScriptAstNode result;

		ActionScriptAstBuilder (ParseTree ast)
		{
			source = ast;
			result = new ActionScriptAstNode ();
		}

		void Build ()
		{
		}
	}

	public abstract class ActionScriptAstNode
	{
		public ActionScriptAstNode (AstNode source)
		{
		}

		public string Name { get; set; }

		public IList<ActionScriptAstNode> SubNodes { get; set; }

		public abstract ActionScriptAstNode Resolve ();
	}
	*/

	public partial class ActionScriptGrammar
	{
		object dummy = new object ();

		void not_implemented (ParsingContext ctx, ParseTreeNode node)
		{
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (ctx, cn);
		}

		void create_ast_binary_operator (ParsingContext ctx, ParseTreeNode node)
		{
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (ctx, cn);
			switch (node.ChildNodes.Count) {
			case 1:
				node.AstNode = node.ChildNodes [0].AstNode;
				break;
			case 3:
				node.AstNode = new BinaryOperation (node.ChildNodes [0].AstNode, node.ChildNodes [2].AstNode, node.ChildNodes [1].AstNode);
				break;
			default:
				throw new Exception (String.Format ("huh? {0} {1}", node.Term.Name, node.ChildNodes.Count));
			}
		}

		void create_ast_simple_list (ParsingContext ctx, ParseTreeNode node)
		{
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (ctx, cn);
			if (node.ChildNodes.Count == 0)
				node.AstNode = new List<object> ();
			else {
				var l = node.ChildNodes [0];
				var list = l.AstNode as List<object>;
				if (list == null) {
					list = new List<object> ();
					list.Add (l.AstNode);
				}
				foreach (var cn in node.ChildNodes)
					if (cn.AstNode != list)
						list.Add (cn.AstNode);
				node.AstNode = list;
			}
		}

		void create_ast_type_name_wild (ParsingContext context, ParseTreeNode parseNode)
		{
			string s = null;
			foreach (var cn in parseNode.ChildNodes) {
				cn.Term.CreateAstNode (context, cn);
				s += cn.AstNode;
			}
			parseNode.AstNode = s;
		}

		void create_ast_qualified_reference (ParsingContext context, ParseTreeNode parseNode)
		{
			string s = null;
			foreach (var cn in parseNode.ChildNodes) {
				cn.Term.CreateAstNode (context, cn);
				s += cn.AstNode;
			}
			parseNode.AstNode = s;
		}

		void create_ast_qualified_reference_1 (ParsingContext context, ParseTreeNode parseNode)
		{
			if (parseNode.ChildNodes.Count != 2) throw new Exception ();
			var l = parseNode.ChildNodes [0];
			var r = parseNode.ChildNodes [1];
			l.Term.CreateAstNode (context, l);
			r.Term.CreateAstNode (context, r);
			string s = l.AstNode + "." + r.AstNode;
			parseNode.AstNode = s;
		}

		void create_ast_qualified_reference_2 (ParsingContext context, ParseTreeNode parseNode)
		{
			string s = null;
			foreach (var cn in parseNode.ChildNodes) {
				cn.Term.CreateAstNode (context, cn);
				s += cn.AstNode;
			}
			parseNode.AstNode = s;
		}

		void create_ast_semi_opt (ParsingContext context, ParseTreeNode parseNode)
		{
			parseNode.AstNode = null;
		}
	}


	public interface ICompileUnitItem
	{
	}

	public partial class CompileUnit
	{
		public CompileUnit ()
		{
			Items = new List<ICompileUnitItem> ();
		}

		public List<ICompileUnitItem> Items { get; private set; }
	}

	public partial class PackageDeclaration : ICompileUnitItem
	{
		public PackageDeclaration (string name, PackageContents items)
		{
			Name = name;
			Items = items;
		}

		public string Name { get; set; }
		public PackageContents Items { get; private set; }
	}

	public partial class EventDeclaration
	{
		public EventDeclaration (TypeName name, EventDeclarationMembers members)
		{
		}
	}
	
	public partial class EventDeclarationMember
	{
		public EventDeclarationMember (Identifier name, Literal value)
		{
		}
	}

	public partial class ClassDeclaration : ICompileUnitItem, IPackageContent
	{
		public ClassDeclaration (EventDeclarations events, MemberHeaders headers, Identifier name, Identifier baseClassName, NamespaceUses namespaceUses, ClassMembers members)
		{
			Events = events;
			Headers = headers;
			Name = name;
			BaseClassName = baseClassName;
			NamespaceUses = namespaceUses;
			Members = members;
		}
		
		public EventDeclarations Events { get; private set; }
		public MemberHeaders Headers { get; private set; }
		public Identifier Name { get; set; }
		public Identifier BaseClassName { get; set; }
		public NamespaceUses NamespaceUses { get; private set; }
		public ClassMembers Members { get; private set; }
	}

	public partial class NamespaceUse
	{
		public NamespaceUse (Identifier name)
		{
			Name = name;
		}
		
		public Identifier Name { get; set; }
	}

	public interface IPackageContent
	{
	}

	public partial class Import : ICompileUnitItem, IPackageContent
	{
		public Import (TypeName name)
		{
			Name = name;
		}

		public TypeName Name { get; set; }
	}

	public partial class NamespaceDeclaration : IPackageContent
	{
		public NamespaceDeclaration (MemberHeaders headers, Identifier name)
		{
			Headers = headers;
			Name = name;
		}

		public MemberHeaders Headers { get; private set; }
		public Identifier Name { get; set; }
	}

	public interface IClassMember
	{
	}

	public partial class ConstantDeclaration : IClassMember
	{
		public ConstantDeclaration (MemberHeaders headers, Identifier name, TypeName typeName, Expression value)
		{
			Headers = headers;
			Name = name;
			TypeName = typeName;
			Value = value;
		}

		public MemberHeaders Headers { get; set; }
		public Identifier Name { get;set; }
		public TypeName TypeName { get; set; }
		public Expression Value { get; set; }
	}

	public partial class FieldDeclaration : IClassMember
	{
		public FieldDeclaration (MemberHeaders headers, NameValuePairs nameValuePairs)
		{
			Headers = headers;
			NameValuePairs = nameValuePairs;
		}

		public MemberHeaders Headers { get; set; }
		public NameValuePairs NameValuePairs { get; set; }
	}

	public partial class GeneralFunction : IClassMember
	{
		public GeneralFunction (MemberHeaders headers, FunctionDefinition definition)
		{
			Headers = headers;
			Definition = definition;
		}
		
		public MemberHeaders Headers { get; set; }
		public FunctionDefinition Definition { get; set; }
	}

	public partial class FunctionDefinition : IExpression, IStatement // could be embedded_function_expression or local_function_declaration
	{
		public FunctionDefinition (ArgumentDeclarations args, TypeName returnType, Statements body)
		{
			Arguments = args;
			ReturnTypeName = returnType;
			Body = body;
		}
		
		public Identifier Name { get; set; } // could be null for anonymous functions
		public ArgumentDeclarations Arguments { get; set; }
		public TypeName ReturnTypeName { get; set; }
		public Statements Body { get; set; }
	}

	public partial class Constructor : FunctionDefinition
	{
		public Constructor (Identifier typeName, ArgumentDeclarations args, Statements body)
			: base (args, typeName, body)
		{
		}
	}

	// statements

	public interface IStatement
	{
	}
	
	public abstract class Statement : IStatement
	{
	}

	public partial class ExpressionStatement : Statement
	{
		public ExpressionStatement (Expression expr)
		{
			Expression = expr;
		}
		
		public Expression Expression { get; set; }
	}

	public enum AssignmentOperators
	{
		Plus,
		Minus,
		Multiply,
		Divide,
		Modulo,
		BitwiseAnd,
		BitwiseOr,
		ShiftLeft,
		ShiftRight
	}

	public interface IForIterator
	{
	}

	public partial class AssignmentExpressionStatement : ExpressionStatement, IForIterator
	{
		public AssignmentExpressionStatement (Expression expr)
			: base (expr)
		{
		}
	}

	public partial class AssignmentStatement : Statement, IForIterator
	{
		public AssignmentStatement (ILeftValue target, AssignmentOperators oper, Expression value)
		{
			Target = target;
			Operator = oper;
			Value = value;
		}
		
		public ILeftValue Target { get; set; }
		public AssignmentOperators Operator { get; set; }
		public Expression Value { get; set; }
	}

	public partial class ReturnStatement : Statement
	{
		public ReturnStatement (Expression value)
		{
			Value = value;
		}
		
		public Expression Value { get; set; }
	}

	public partial class IfStatement : Statement
	{
		public IfStatement (Expression cond, Statement trueStatement, Statement falseStatement)
		{
			Condition = cond;
			TrueStatement = trueStatement;
			FalseStatement = falseStatement;
		}
		
		public Expression Condition { get; set; }
		public Statement TrueStatement { get; set; }
		public Statement FalseStatement { get; set; }
	}

	public partial class SwitchStatement : Statement
	{
		public SwitchStatement (Expression cond, SwitchBlocks caseBlocks)
		{
			Condition = cond;
			CaseBlocks = caseBlocks;
		}

		public Expression Condition { get; set; }
		public SwitchBlocks CaseBlocks { get; set; }
	}

	public partial class SwitchBlock
	{
		public static readonly object Default = new object ();

		public SwitchBlock (object label, Statements statements)
		{
			Label = label;
			Statements = statements;
		}
		
		public object Label { get; set; }
		public Statements Statements { get; set; }
	}

	public partial class WhileStatement : Statement
	{
		public WhileStatement (Expression cond, Statement body)
		{
		}
	}
	
	public partial class DoWhileStatement : Statement
	{
		public DoWhileStatement (Statement body, Expression cond)
		{
		}
	}

	public partial class ForStatement : Statement
	{
		public ForStatement (ForInitializers init, Expression cond, ForIterators iter, Statement body)
		{
		}
	}

	public partial class ForInStatement : Statement
	{
		public ForInStatement (ForEachIterator iter, Expression cond, Statement body)
		{
		}
	}

	public partial class ForInitializers
	{
		public ForInitializers (LocalVariableDeclarationStatement vardecl)
		{
		}

		public ForInitializers (ForAssignStatements assigns)
		{
		}
	}

	public partial class ForEachIterator
	{
		public ForEachIterator (Identifier existingIdent)
		{
		}

		public ForEachIterator (Identifier newLocalVar, TypeName type)
		{
		}
	}

	public partial class BlockStatement : Statement
	{
		public BlockStatement (Statements stmts)
		{
		}
	}

	public partial class DeleteStatement : Statement
	{
		public DeleteStatement (Expression target)
		{
		}
	}

	// expressions

	public interface IExpression
	{
	}

	public abstract class Expression : IExpression
	{
	}

	public partial class ConditionalExpression : Expression
	{
		public ConditionalExpression (Expression cond, Expression trueExpr, Expression falseExpr)
		{
		}
	}
	
	public partial class BinaryExpression : Expression
	{
		public BinaryExpression (Expression left, Expression right, Operators oper)
		{
		}
	}
	
	public partial class UnaryExpression : Expression
	{
		public UnaryExpression (Expression primary, Operators oper)
		{
		}
	}

	public partial class IncrementDecrementExpression : UnaryExpression
	{
		public IncrementDecrementExpression (Expression primary, Operators oper, bool isPostfix)
			: base (primary, oper)
		{
		}
	}
	
	public interface ILeftValue // things that can be lvalue
	{
	}
	
	public partial class ArrayAccessExpression : Expression, ILeftValue
	{
		public ArrayAccessExpression (Expression array, Expression index)
		{
		}
	}

	public partial class ParenthesizedExpression : Expression
	{
		public ParenthesizedExpression (Expression content)
		{
		}
	}

	public partial class FunctionCallExpression : Expression
	{
		public FunctionCallExpression (Expression member, FunctionCallArguments args)
		{
		}
	}

	public partial class ArgumentDeclaration
	{
		public ArgumentDeclaration (Identifier name, TypeName type, Expression defaultValue)
		{
		}
	}

	public partial class NameReferenceExpression : Expression, ILeftValue
	{
		public NameReferenceExpression (NameReference target)
		{
		}
	}

	public enum MemberAccessType
	{
		Instance,
		Static,
		GenericSubtype
	}

	public partial class NameReference
	{
		public NameReference (Identifier member)
		{
		}

		public NameReference (Expression target, Identifier member, MemberAccessType accessType)
		{
		}

		//public NameReference (Expression target, TypeName genericSubtype, MemberAccessType accessType)
		//{
		//}
	}

	public partial class NewObjectExpression : Expression
	{
		public NewObjectExpression (TypeName name, FunctionCallArguments args)
		{
		}
	}
	
	public partial class LiteralArrayExpression : Expression
	{
		public LiteralArrayExpression (Expressions values)
		{
		}
	}

	public partial class LiteralHashExpression : Expression
	{
		public LiteralHashExpression (HashItems values)
		{
		}
	}

	public partial class HashItem
	{
		public HashItem (Identifier key, Expression value)
		{
		}

		public HashItem (Literal key, Expression value)
		{
		}
	}

	public partial class EmbeddedFunctionExpression : Expression
	{
		public EmbeddedFunctionExpression (FunctionDefinition func)
		{
		}
	}

	public enum Operators
	{
		Equality,
		Equality2,
		Inequality,
		Inequality2,
		Less,
		LessEqual,
		Greater,
		GreaterEqual,
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulo,
		ShiftLeft,
		RotateLeft,
		ShiftRight,
		RotateRight,
		UnionAnd,
		UnionOr,
		UnionXor,
		LogicalOr,
		LogicalAnd,
		LogicalNot,
		Minus,
		Complement,
		Plus,
		IterateIn,
		TypeIs,
		CastAs,
	}

	public partial class NameValuePair
	{
		public NameValuePair (Identifier key, Expression value)
		{
		}
	}

	public partial class LocalVariableDeclarationStatement
	{
		public LocalVariableDeclarationStatement (NameValuePairs nameValuePairs)
		{
		}
	}

	public partial class PropertyGetter
	{
		public PropertyGetter (MemberHeaders headers, Identifier name, TypeName typeName, Statements statements)
		{
		}
	}

	public partial class PropertySetter
	{
		public PropertySetter (MemberHeaders headers, Identifier propName, Identifier argName, TypeName typeName, Statements statements)
		{
		}
	}

	public partial class BreakStatement : Statement
	{
	}
	
	public partial class ContinueStatement : Statement
	{
	}
	
	public partial class VarargDeclaration
	{
	}
	
	public partial class ForEachStatement : Statement
	{
		public ForEachStatement (ForEachIterator iter, Expression targetExpr, Statement stmt)
		{
		}
	}

	public partial class ThrowStatement : Statement
	{
		public ThrowStatement (Expression target)
		{
		}
	}

	public partial class TryStatement : Statement
	{
		public TryStatement (Statements statements, CatchBlock catchBlock, Statements finallyBlock)
		{
		}
	}
	
	public partial class CatchBlock
	{
		public CatchBlock (TypedIdentifier nameAndType, Statements statements)
		{
		}
	}

	public partial class AssignmentExpression : Expression
	{
		public AssignmentExpression (ILeftValue lvalue, Expression rvalue)
		{
		}
	}
}
