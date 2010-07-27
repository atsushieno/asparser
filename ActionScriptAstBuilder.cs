using System;
using System.Collections.Generic;
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
//using EventDeclarationMembers = System.Collections.Generic.List<FreeActionScript.EventDeclarationMember>;
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
	public partial class ActionScriptGrammar
	{
		protected void ProcessChildrenCommon (ParsingContext ctx, ParseTreeNode node, params int [] expectedChildCounts)
		{
			if (expectedChildCounts.Length > 0 && Array.IndexOf (expectedChildCounts, node.ChildNodes.Count) < 0)
				throw new Exception (String.Format ("Node {0} is expected to have {1} child nodes, but there was {2}", node.Term.Name, expectedChildCounts.ConcatToString (" or "), node.ChildNodes.Count));
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (ctx, cn);
		}

		void not_implemented (ParsingContext ctx, ParseTreeNode node)
		{
			Console.WriteLine ("Node {0} has {1} children", node.Term.Name, node.ChildNodes.Count);
			foreach (var cn in node.ChildNodes)
				cn.Term.CreateAstNode (ctx, cn);
		}

		void create_ast_select_single_child (ParsingContext context, ParseTreeNode parseNode)
		{
			if (parseNode.ChildNodes.Count != 1)
				throw new Exception (String.Format ("On {0}, expected 1 child but had {1} children", parseNode.Term.Name, parseNode.ChildNodes.Count));
		}

		void create_ast_binary_operator (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 3);
			if (node.ChildNodes.Count == 1)
				node.AstNode = node.Get<object> (0);
			if (node.ChildNodes.Count == 3)
				node.AstNode = new BinaryExpression (node.Get<Expression> (0), node.Get<Expression> (2), node.ChildNodes [1].Term.Name);
		}

		void create_ast_simple_list<T> (ParsingContext ctx, ParseTreeNode node)
		{
			ProcessChildrenCommon (ctx, node);
			if (node.ChildNodes.Count == 0)
				node.AstNode = new List<T> ();
			else {
				var l = node.ChildNodes [0];
				var list = l.AstNode as List<T>;
				if (list == null)
					list = new List<T> ();
				foreach (var cn in node.ChildNodes)
					if (cn.AstNode != list) {
						if (cn.AstNode is T)
							list.Add ((T) cn.AstNode);
						else throw new Exception (String.Format ("On node {2}, child AstNode is {0}, not {1}", cn.AstNode, typeof (T), node.Term.Name));
					}
				node.AstNode = list;
			}
		}

		// specific creation rules

		void create_ast_compile_unit (ParsingContext context, ParseTreeNode parseNode)
		{
			var cu = new CompileUnit ();
			foreach (var cn in parseNode.ChildNodes) {
				if (cn.AstNode == null)
					cn.Term.CreateAstNode (context, cn);
				cu.Items.Add ((ICompileUnitItem) cn.AstNode);
			}
			parseNode.AstNode = cu;
		}

		void create_ast_package_decl (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			node.AstNode = new PackageDeclaration (node.Get<string> (1), node.Get<PackageContents> (2));
		}

		void create_ast_access_modifier (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = node.ChildNodes [0].Term.Name;
		}

		void create_ast_namespace_or_class (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			var cns = node.Get<INamespaceOrClass> (2);
			cns.Events = node.Get<EventDeclarations> (0);
			cns.Headers = node.Get<MemberHeaders> (1);
			node.AstNode = cns;
		}

		void create_ast_namespace_decl (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new NamespaceDeclaration (node.Get<Identifier> (1));
		}

		void create_ast_namespace_use (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			node.AstNode = new NamespaceUse (node.Get<Identifier> (2));
		}

		void create_ast_import (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new Import (node.Get<TypeName> (1));
		}

		void create_ast_class_decl (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 5);
			node.AstNode = new ClassDeclaration (node.Get<Identifier> (1), node.GetNullable<TypeName> (2), node.Get<NamespaceUses> (3), node.Get<ClassMembers> (4));
		}

		void create_ast_event_decl (ParsingContext context, ParseTreeNode node) 
		{
			ProcessChildrenCommon (context, node, 4);
			node.AstNode = new EventDeclaration (node.Get<TypeName> (0), node.Get<NameTypeValues> (2));
		}

		void create_ast_event_decl_member (ParsingContext context, ParseTreeNode node) 
		{
			ProcessChildrenCommon (context, node, 3);
			node.AstNode = new NameTypeValue (node.Get<Identifier> (0), null, node.Get<Literal> (2));
		}

		void create_ast_field_declaration (ParsingContext context, ParseTreeNode node) 
		{
			ProcessChildrenCommon (context, node, 4);
			node.AstNode = new FieldDeclaration (node.Get<MemberHeaders> (0), node.Get<NameTypeValues> (2));
		}

		void create_ast_constant_declaration (ParsingContext context, ParseTreeNode node) 
		{
			ProcessChildrenCommon (context, node, 4);
			node.AstNode = new ConstantDeclaration (node.Get<MemberHeaders> (0), node.Get<NameTypeValues> (2));
		}

		void create_ast_constructor (ParsingContext context, ParseTreeNode node) 
		{
			ProcessChildrenCommon (context, node, 6);
			var fd = new FunctionDefinition (node.Get<Identifier> (1), node.Get<ArgumentDeclarations> (3), null, node.Get<BlockStatement> (5));
			node.AstNode = new Constructor (fd);
		}

		void create_ast_general_function (ParsingContext context, ParseTreeNode node) 
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new GeneralFunction (node.Get<MemberHeaders> (0), node.Get<FunctionDefinition> (1));
		}

		void create_ast_general_function_headless (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			var fd = node.Get<FunctionDefinition> (2);
			fd.Name = node.Get<Identifier> (1);
			node.AstNode = fd;
		}

		void create_ast_function_nameless (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 4, 5);
			if (node.ChildNodes.Count == 4)
				node.AstNode = new FunctionDefinition (node.Get<ArgumentDeclarations> (1), null, node.Get<BlockStatement> (3));
			else
				node.AstNode = new FunctionDefinition (node.Get<ArgumentDeclarations> (1), node.GetNullable<TypeName> (3), node.Get<BlockStatement> (4));
		}

		void create_ast_argument_decl (ParsingContext context, ParseTreeNode node)
		{
			// identifier + ":" + argument_type + assignment_opt | varargs_decl
			ProcessChildrenCommon (context, node, 1, 2, 3);

			if (node.ChildNodes.Count == 1) // vardecl
				node.AstNode = node.Get<ArgumentDeclaration> (0);
			else if (node.ChildNodes.Count == 2)
				node.AstNode = new ArgumentDeclaration (node.Get<Identifier> (0), node.Get<TypeName> (1), null);
			else if (node.ChildNodes.Count == 3)
				node.AstNode = new ArgumentDeclaration (node.Get<Identifier> (0), node.Get<TypeName> (1), node.GetNullable<Expression> (2));
		}

		void create_ast_varargs_decl (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new ArgumentDeclaration (node.Get<Identifier> (1));
		}

		void create_ast_assignment_opt (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 0, 2);
			if (node.ChildNodes.Count == 2)
				node.AstNode = node.Get<Expression> (1);
		}

		void create_ast_property_getter (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 8);

			node.AstNode = new PropertyGetter (node.Get<MemberHeaders> (0), node.Get<Identifier> (3), node.Get<TypeName> (6), node.Get<BlockStatement> (7));
		}

		void create_ast_property_setter (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 10);
			node.AstNode = new PropertySetter (node.Get<MemberHeaders> (0), node.Get<Identifier> (3), node.Get<Identifier> (5), node.Get<TypeName> (6), node.Get<BlockStatement> (9));
		}

		void create_ast_statement_lacking_colon_then_colon (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = node.ChildNodes [0].AstNode;
		}

		void create_ast_return_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 2);
			node.AstNode = new ReturnStatement (node.ChildNodes.Count == 1 ? null : node.Get<Expression> (1));
		}

		void create_ast_block_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = new BlockStatement (node.Get<Statements> (0));
		}

		void create_ast_if_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 6);
			var cond = node.Get<Expression> (2);
			var t = node.Get<Statement> (4);
			var f = node.GetNullable<Statement> (5);
			node.AstNode = new IfStatement (cond, t, f);
		}

		void create_ast_else_block (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 0, 2);
			if (node.ChildNodes.Count == 0)
				return; // empty
			node.AstNode = node.Get<Statement> (1);
		}

		void create_ast_switch_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 5);
			var cond = node.Get<Expression> (2);
			var blocks = node.Get<SwitchBlocks> (4);
			node.AstNode = new SwitchStatement (cond, blocks);
		}

		void create_ast_switch_cond_block (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new SwitchBlock (node.Get<object> (0), node.Get<Statements> (1));
		}

		void create_ast_condition_label (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 2);
			if (node.ChildNodes.Count == 1)
				node.AstNode = SwitchBlock.Default;
			else
				node.AstNode = node.Get<object> (1);
		}

		void create_ast_for_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			node.AstNode = node.Get<Statement> (2); // ForStatement or ForInStatement
		}

		void create_ast_for_in_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 5);
			node.AstNode = new ForInStatement (node.Get<ForEachIterator> (0), node.Get<Expression> (2), node.Get<Statement> (4));
		}

		void create_ast_for_c_style_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 5);
			node.AstNode = new ForStatement (node.Get<ForInitializers> (0), node.Get<Expression> (1), node.Get<ForIterators> (2), node.Get<Statement> (4));
		}

		void create_ast_for_initializers (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 0, 1);
			if (node.Get<object> (0) is ForAssignStatements)
				node.AstNode = new ForInitializers (node.Get<ForAssignStatements> (0));
			else
				node.AstNode = new ForInitializers (node.Get<LocalVariableDeclarationStatement> (0));
		}

		void create_ast_while_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 5);
			node.AstNode = new WhileStatement (node.Get<Expression> (2), node.Get<Statement> (4));
		}

		void create_ast_do_while_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 6);
			node.AstNode = new DoWhileStatement (node.Get<Statement> (1), node.Get<Expression> (4));
		}

		void create_ast_break_statement (ParsingContext context, ParseTreeNode node)
		{
			node.AstNode = new BreakStatement ();
		}

		void create_ast_continue_statement (ParsingContext context, ParseTreeNode node)
		{
			node.AstNode = new ContinueStatement ();
		}

		void create_ast_for_each_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 8);
			node.AstNode = new ForEachStatement (node.Get<ForEachIterator> (3), node.Get<Expression> (5), node.Get<Statement> (7));
		}

		void create_ast_for_each_iterator (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 3);
			if (node.ChildNodes.Count == 1)
				node.AstNode = new ForEachIterator (node.Get<Identifier> (0));
			else
				node.AstNode = new ForEachIterator (node.Get<Identifier> (1), node.Get<TypeName> (2));
		}

		void create_ast_throw_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new ThrowStatement (node.Get<Expression> (1));
		}

		void create_ast_try_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 4);
			node.AstNode = new TryStatement (node.Get<BlockStatement> (1), node.Get<CatchBlocks> (2), node.GetNullable<BlockStatement> (3));
		}

		void create_ast_catch_block (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			node.AstNode = new CatchBlock (node.Get<TypedIdentifier> (1), node.Get<BlockStatement> (2));
		}

		void create_ast_exception_type_part (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 0, 4);
			node.AstNode = node.ChildNodes.Count == 0 ? null : new TypedIdentifier (node.Get<Identifier> (1), node.Get<TypeName> (2));
		}

		void create_ast_finally_block (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 0, 2);
			node.AstNode = node.ChildNodes.Count == 0 ? null : node.Get<BlockStatement> (1);
		}

		void create_ast_local_var_decl_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new LocalVariableDeclarationStatement (node.Get<NameTypeValues> (1));
		}

		void create_ast_expression_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = new ExpressionStatement (node.Get<Expression> (0));
		}

		void create_ast_conditional_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 3);
			if (node.ChildNodes.Count == 1)
				node.AstNode = node.Get<Expression> (0);
			if (node.ChildNodes.Count == 3)
				node.AstNode = new ConditionalExpression (node.Get<Expression> (0), node.Get<Expression> (1), node.Get<Expression> (2));
		}

		void create_ast_as_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 3);
			if (node.ChildNodes.Count == 1)
				node.AstNode = node.Get<Expression> (0);
			if (node.ChildNodes.Count == 3)
				node.AstNode = new CastAsExpression (node.Get<Expression> (0), node.Get<TypeName> (2));
		}

		void create_ast_function_call_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 4);
			node.AstNode = new FunctionCallExpression (node.Get<Expression> (0), node.Get<FunctionCallArguments> (2));
		}

		void create_ast_name_type_value (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			node.AstNode = new NameTypeValue (node.Get<Identifier> (0), node.GetNullable<TypeName> (1), node.GetNullable<Expression> (2));
		}

		void create_ast_assign_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = new AssignmentExpressionStatement (node.Get<Expression> (0));
		}

		void create_ast_calc_assign_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 3);
			if (node.ChildNodes.Count == 1)
				node.AstNode = new AssignmentExpressionStatement (node.Get<Expression> (0));
			if (node.ChildNodes.Count == 3)
				node.AstNode = new CalcAssignStatement (node.Get<ILeftValue> (0), node.Get<Expression> (2), node.ChildNodes [1].Term.Name);
		}

		void create_ast_assignment_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 3);
			node.AstNode = new AssignmentExpression (node.Get<ILeftValue> (0), node.Get<Expression> (2));
		}

		void create_ast_iteration_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 3);
			if (node.ChildNodes.Count == 1)
				node.AstNode = node.Get<Expression> (0);
			if (node.ChildNodes.Count == 3)
				node.AstNode = new ArrayInExpression (node.Get<Expression> (0), node.Get<Expression> (2));
		}

//Console.Write (":: " + node.Term.Name); foreach (var cn in node.ChildNodes) Console.Write (" " + cn.Term.Name + "(" + cn.AstNode + ")"); Console.WriteLine ();

		void create_ast_inc_dec_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			if (node.GetNullable<object> (0) is Expression)
				node.AstNode = new IncrementDecrementExpression (node.ChildNodes [1].Term.Name, node.Get<Expression> (0), true);
			else
				node.AstNode = new IncrementDecrementExpression (node.ChildNodes [0].Term.Name, node.Get<Expression> (1), false);
		}

		void create_ast_array_access_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 2);
			if (node.ChildNodes.Count == 1)
				node.AstNode = node.Get<Expression> (0);
			if (node.ChildNodes.Count == 2)
				node.AstNode = new ArrayAccessExpression (node.Get<Expression> (0), node.Get<Expression> (1));
		}

		void create_ast_unary_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 2);
			if (node.ChildNodes.Count == 1)
				node.AstNode = node.Get<Expression> (0);
			if (node.ChildNodes.Count == 2)
				node.AstNode = new UnaryExpression (node.ChildNodes [0].Term.Name, node.Get<Expression> (1));
		}

		void create_ast_primary_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 3);
			if (node.ChildNodes.Count == 1) {
				node.AstNode = node.Get<object> (0);
				/*
				var obj = node.Get<object> (0);
				if (obj is Literal)
					node.AstNode = new ConstantExpression ((Literal) obj);
				else
					node.AstNode = obj;
				*/
			}
			else
				node.AstNode = new ParenthesizedExpression (node.Get<Expression> (1));
		}

		void create_ast_embedded_function_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new EmbeddedFunctionExpression (node.Get<FunctionDefinition> (1));
		}

		void create_ast_local_function_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = new LocalFunctionStatement (node.Get<FunctionDefinition> (0));
		}

		void create_ast_member_reference_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 2);
			if (node.ChildNodes.Count == 1)
				node.AstNode = new MemberReferenceExpression (node.Get<MemberReference> (0));
			if (node.ChildNodes.Count == 2)
				node.AstNode = new ArrayAccessExpression (node.Get<Expression> (0), node.Get<Expression> (1));
		}

		void create_ast_member_reference (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 2, 3, 4);
			if (node.ChildNodes.Count == 1)
				node.AstNode = new MemberReference (node.Get<Identifier> (0));
			else if (node.ChildNodes.Count == 2)
				node.AstNode = new MemberReference (node.Get<Expression> (0), node.Get<Identifier> (1), MemberAccessType.Instance);
			else if (node.ChildNodes.Count == 3)
				node.AstNode = new MemberReference (node.Get<Expression> (0), node.Get<Identifier> (2), MemberAccessType.Static);
			else
				node.AstNode = new MemberReference (node.Get<Expression> (0), node.Get<TypeName> (2), MemberAccessType.GenericSubtype);
		}

		void create_ast_type_name_wild (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node);
			string s = null;
			foreach (var cn in node.ChildNodes) {
				if (cn.AstNode is TypeName)
					s += cn.AstNode;
				else
					s += cn.Term.Name;
			}
			node.AstNode = new TypeName (s);
		}

		void create_ast_type_name (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node);
			string s = null;
			foreach (var cn in node.ChildNodes)
				s += cn.AstNode;
			node.AstNode = new TypeName (s);
		}

		void create_ast_qualified_reference (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1, 2, 4);
			if (node.ChildNodes.Count == 1)
				node.AstNode = node.Get<string> (0);
			else if (node.ChildNodes.Count == 2)
				node.AstNode = node.Get<string> (0) + "." + node.Get<string> (1);
			else if (node.ChildNodes.Count == 4)
				node.AstNode = node.Get<string> (0) + ".<" + node.Get<TypeName> (2).Raw + ">";
		}

		void create_ast_semi_opt (ParsingContext context, ParseTreeNode node)
		{
			node.AstNode = null;
		}

		void create_ast_literal (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			if (node.ChildNodes [0].Term.Name == "null")
				node.AstNode = new Literal (null);
			else
				node.AstNode = new Literal (node.Get<object> (0));
		}

		void create_ast_new_object_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 5);
			node.AstNode = new NewObjectExpression (node.Get<TypeName> (1), node.Get<FunctionCallArguments> (3));
		}

		void create_ast_delete_statement (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			node.AstNode = new DeleteStatement (node.Get<Expression> (1));
		}

		void create_ast_literal_array_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = new LiteralArrayExpression (node.Get<Expressions> (0));
		}

		void create_ast_literal_hash_expression (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 1);
			node.AstNode = new LiteralHashExpression (node.Get<HashItems> (0));
		}

		void create_ast_hash_item (ParsingContext context, ParseTreeNode node)
		{
			ProcessChildrenCommon (context, node, 2);
			if (node.Get<object> (0) is Literal)
				node.AstNode = new HashItem (node.Get<Literal> (0), node.Get<Expression> (1));
			else
				node.AstNode = new HashItem (node.Get<Identifier> (0), node.Get<Expression> (1));
		}
	}


	public partial interface ICompileUnitItem
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

	public interface INamespaceOrClass
	{
		EventDeclarations Events { get; set; }
		MemberHeaders Headers { get; set; }
	}

	public partial class EventDeclaration
	{
		public EventDeclaration (TypeName name, NameTypeValues members)
		{
			Name = name;
			Members = members;
		}

		public TypeName Name { get; set; }
		public NameTypeValues Members { get; set; }
	}
	
	/*
	public partial class EventDeclarationMember
	{
		public EventDeclarationMember (Identifier name, Literal value)
		{
			Name = name;
			Value = value;
		}
		
		public Identifier Name { get; set; }
		public Literal Value { get; set; }
	}
	*/

	public partial class ClassDeclaration : ICompileUnitItem, IPackageContent, INamespaceOrClass
	{
		public ClassDeclaration (Identifier name, TypeName baseClassName, NamespaceUses namespaceUses, ClassMembers members)
		{
			Name = name;
			BaseClassName = baseClassName;
			NamespaceUses = namespaceUses;
			Members = members;
		}
		
		public EventDeclarations Events { get; set; }
		public MemberHeaders Headers { get; set; }
		public Identifier Name { get; set; }
		public TypeName BaseClassName { get; set; }
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

	public partial interface IPackageContent
	{
	}

	public partial class Import : ICompileUnitItem, IPackageContent
	{
		public Import (TypeName type)
		{
			Type = type;
		}

		public TypeName Type { get; set; }
	}

	public partial class NamespaceDeclaration : IPackageContent, INamespaceOrClass
	{
		public NamespaceDeclaration (Identifier name)
		{
			Name = name;
		}

		public EventDeclarations Events { get; set; }
		public MemberHeaders Headers { get; set; }
		public Identifier Name { get; set; }
	}

	public partial interface IClassMember
	{
	}

	public abstract partial class ClassMemberBase : IClassMember
	{
		public ClassMemberBase (MemberHeaders headers)
		{
			Headers = headers;
		}

		public MemberHeaders Headers { get; set; }
	}

	public partial class FieldDeclaration : ClassMemberBase
	{
		public FieldDeclaration (MemberHeaders headers, NameTypeValues nameValuePairs)
			: base (headers)
		{
			NameTypeValues = nameValuePairs;
		}

		public NameTypeValues NameTypeValues { get; set; }
	}

	public partial class ConstantDeclaration : FieldDeclaration
	{
		public ConstantDeclaration (MemberHeaders headers, NameTypeValues nameValuePairs)
			: base (headers, nameValuePairs)
		{
		}
	}

	public partial class GeneralFunction : ClassMemberBase
	{
		public GeneralFunction (MemberHeaders headers, FunctionDefinition definition)
			: base (headers)
		{
			Definition = definition;
		}
		
		public FunctionDefinition Definition { get; set; }
	}

	public partial class Constructor : GeneralFunction
	{
		public Constructor (FunctionDefinition definition)
			: base (null, definition)
		{
		}
	}

	public partial class FunctionDefinition : IExpression // could be embedded_function_expression
	{
		public FunctionDefinition (ArgumentDeclarations args, TypeName returnType, BlockStatement body)
		{
			if (args == null)
				throw new ArgumentNullException ("args");
			if (body == null)
				throw new ArgumentNullException ("body");
			Arguments = args;
			ReturnTypeName = returnType;
			Body = body;
		}
		
		public FunctionDefinition (Identifier name, ArgumentDeclarations args, TypeName returnType, BlockStatement body)
			: this (args, returnType, body)
		{
			Name = name;
		}

		public Identifier Name { get; set; } // could be null for anonymous functions
		public ArgumentDeclarations Arguments { get; set; }
		public TypeName ReturnTypeName { get; set; }
		public BlockStatement Body { get; set; }
	}

	// statements
	
	public abstract partial class Statement
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

	public partial interface IForIterator
	{
	}

	public interface ICalcAssignStatement : IForIterator
	{
	}

	public partial class AssignmentExpressionStatement : ExpressionStatement, ICalcAssignStatement
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

		// Identifier, Literal or Default above.
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
			Condition = cond;
			Body = body;
		}

		public Expression Condition { get; set; }
		public Statement Body { get; set; }
	}
	
	public partial class DoWhileStatement : Statement
	{
		public DoWhileStatement (Statement body, Expression cond)
		{
			Body = body;
			Condition = cond;
		}

		public Expression Condition { get; set; }
		public Statement Body { get; set; }
	}

	public partial class ForStatement : Statement
	{
		public ForStatement (ForInitializers init, Expression cond, ForIterators iter, Statement body)
		{
			Initializers = init;
			Condition = cond;
			Iterators = iter;
			Body = body;
		}
		
		public ForInitializers Initializers {get; set; }
		public Expression Condition { get; set; }
		public ForIterators Iterators { get; set; }
		public Statement Body { get; set; }
	}

	public partial class ForInStatement : Statement
	{
		public ForInStatement (ForEachIterator iter, Expression target, Statement body)
		{
			Iterator = iter;
			Target = target;
			Body = body;
		}
		
		public ForEachIterator Iterator { get; set; }
		public Expression Target { get; set; }
		public Statement Body { get; set; }
	}

	public partial class ForInitializers
	{
		public ForInitializers (LocalVariableDeclarationStatement vardecl)
		{
			LocalVariables = vardecl;
		}

		public ForInitializers (ForAssignStatements assigns)
		{
			AssignStatements = assigns;
		}

		LocalVariableDeclarationStatement LocalVariables { get; set; }
		ForAssignStatements AssignStatements { get; set; }
	}

	public partial class ForEachIterator
	{
		public ForEachIterator (Identifier existingIdent)
		{
			Name = existingIdent;
		}

		public ForEachIterator (Identifier newLocalVar, TypeName type)
		{
			Name = newLocalVar;
			LocalVariableType = type;
		}

		public Identifier Name { get; set; }
		public TypeName LocalVariableType { get; set; }
	}

	public partial class BlockStatement : Statement
	{
		public BlockStatement (Statements stmts)
		{
			Statements = stmts;
		}
		
		public Statements Statements { get; set; }
	}

	public partial class DeleteStatement : Statement
	{
		public DeleteStatement (Expression target)
		{
			Target = target;
		}
		
		public Expression Target { get; set; }
	}

	// expressions

	public interface IExpression
	{
	}

	public abstract partial class Expression : IExpression
	{
	}

	public partial class ConditionalExpression : Expression
	{
		public ConditionalExpression (Expression cond, Expression trueValue, Expression falseValue)
		{
			Condition = cond;
			TrueValue = trueValue;
			FalseValue = falseValue;
		}

		public Expression Condition { get; set; }
		public Expression TrueValue { get; set; }
		public Expression FalseValue { get; set; }
	}
	
	public partial class BinaryExpression : Expression
	{
		public BinaryExpression (Expression left, Expression right, string oper)
		{
			if (oper == null)
				throw new ArgumentNullException ("oper");
			if (left == null)
				throw new ArgumentNullException ("left", String.Format ("Left branch missing on operator {0}", oper));
			if (right == null)
				throw new ArgumentNullException ("right", String.Format ("Left branch missing on operator {0}", oper));
			Left = left;
			Right = right;
			Operator = oper;
		}
		
		public Expression Left { get; set; }
		public Expression Right { get; set; }
		public string Operator { get; set; }
	}
	
	public partial class UnaryExpression : Expression
	{
		public UnaryExpression (string oper, Expression primary)
		{
			Operator = oper;
			Primary = primary;
		}

		public Expression Primary { get; set; }
		public string Operator { get; set; }
	}

	public partial class IncrementDecrementExpression : UnaryExpression, ICalcAssignStatement
	{
		public IncrementDecrementExpression (string oper, Expression primary, bool isPostfix)
			: base (oper, primary)
		{
			IsPostfix = isPostfix;
		}

		public bool IsPostfix { get; set; }
	}
	
	public partial interface ILeftValue // things that can be lvalue
	{
	}
	
	public partial class ArrayAccessExpression : Expression, ILeftValue
	{
		public ArrayAccessExpression (Expression array, Expression index)
		{
			Array = array;
			Index = index;
		}
		
		public Expression Array { get; set; }
		public Expression Index { get; set; }
	}

	public partial class ArrayInExpression : Expression, ILeftValue
	{
		public ArrayInExpression (Expression threshold, Expression array)
		{
			Array = array;
			Threshold = threshold;
		}

		public Expression Array { get; set; }
		public Expression Threshold { get; set; }
	}

	public partial class ParenthesizedExpression : Expression
	{
		public ParenthesizedExpression (Expression content)
		{
			Content = content;
		}

		public Expression Content { get; set; }
	}

	public partial class FunctionCallExpression : Expression
	{
		public FunctionCallExpression (Expression target, FunctionCallArguments args)
		{
			Target = target;
			Arguments = args;
		}

		public Expression Target { get; set; }
		public FunctionCallArguments Arguments { get; set; }
	}

	public partial class CastAsExpression : Expression
	{
		public CastAsExpression (Expression primary, TypeName type)
		{
			Primary = primary;
			Type = type;
		}
		
		public Expression Primary { get; set; }
		public TypeName Type { get; set; }
	}

	public partial class ArgumentDeclaration
	{
		public ArgumentDeclaration (Identifier varArgName)
		{
			Name = varArgName;
			IsVarArg = true;
		}

		public ArgumentDeclaration (Identifier name, TypeName type, Expression defaultValue)
		{
			Name = name;
			Type = type;
			DefaultValue = defaultValue;
		}

		public Identifier Name { get; set; }
		public TypeName Type { get; set; }
		public Expression DefaultValue { get; set; }
		public bool IsVarArg { get; private set; }
	}

	public enum MemberAccessType
	{
		Instance,
		Static,
		GenericSubtype
	}

	public partial class MemberReferenceExpression : Expression, ILeftValue
	{
		public MemberReferenceExpression (MemberReference target)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			Target = target;
		}

		public MemberReference Target { get; set; }
	}

	public partial class MemberReference
	{
		public MemberReference (Identifier member)
		{
			if (member == null)
				throw new ArgumentNullException ("member");
			Member = member;
		}

		public MemberReference (Expression target, Identifier member, MemberAccessType accessType)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			if (member == null)
				throw new ArgumentNullException ("member");
			Target = target;
			Member = member;
			AccessType = accessType;
		}

		public MemberReference (Expression target, TypeName genericSubtype, MemberAccessType accessType)
		{
			if (target == null)
				throw new ArgumentNullException ("target");
			if (genericSubtype == null)
				throw new ArgumentNullException ("genericSubtype");
			Target = target;
			GenericSubtype = genericSubtype;
			AccessType = accessType;
		}

		public Expression Target { get; set; }
		public Identifier Member { get; set; }
		public TypeName GenericSubtype { get; set; }
		public MemberAccessType AccessType { get; set; }
	}

	public partial class NewObjectExpression : Expression
	{
		public NewObjectExpression (TypeName name, FunctionCallArguments args)
		{
			Name = name;
			Arguments = args;
		}
		
		public TypeName Name { get; set; }
		public FunctionCallArguments Arguments { get; set; }
	}
	
	public partial class LiteralArrayExpression : Expression
	{
		public LiteralArrayExpression (Expressions values)
		{
			Values = values;
		}
		
		public Expressions Values { get; set; }
	}

	public partial class LiteralHashExpression : Expression
	{
		public LiteralHashExpression (HashItems values)
		{
			Values = values;
		}
		
		public HashItems Values { get; set; }
	}

	public partial class HashItem
	{
		public HashItem (Identifier key, Expression value)
		{
			Key = key;
			Value = value;
		}

		public HashItem (Literal key, Expression value)
		{
			Key = key;
			Value = value;
		}
		
		public object Key { get; set; }
		public Expression Value { get; set; }
	}

	public partial class EmbeddedFunctionExpression : Expression
	{
		public EmbeddedFunctionExpression (FunctionDefinition func)
		{
			Function = func;
		}
		
		public FunctionDefinition Function { get; set; }
	}

	public partial class LocalFunctionStatement : Statement
	{
		public LocalFunctionStatement (FunctionDefinition func)
		{
			Function = func;
		}
		
		public FunctionDefinition Function { get; set; }
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

	public partial class TypedIdentifier
	{
		public TypedIdentifier (Identifier name, TypeName type)
		{
			Name = name;
			Type = type;
		}
		
		public Identifier Name { get; set; }
		public TypeName Type { get; set; }
	}

	public partial class NameTypeValue
	{
		public NameTypeValue (Identifier name, TypeName type, Expression value)
		{
			Name = name;
			Type = type;
			Value = value;
		}
		
		public Identifier Name { get; set; }
		public TypeName Type { get; set; }
		public Expression Value { get; set; }
	}

	public partial class LocalVariableDeclarationStatement : Statement
	{
		public LocalVariableDeclarationStatement (NameTypeValues nameValuePairs)
		{
			Pairs = nameValuePairs;
		}
		
		NameTypeValues Pairs { get; set; }
	}

	public partial class PropertyGetter : GeneralFunction
	{
		public PropertyGetter (MemberHeaders headers, Identifier name, TypeName typeName, BlockStatement body)
			: base (headers, new FunctionDefinition (name, new ArgumentDeclarations (), typeName, body))
		{
		}
	}

	public partial class PropertySetter : GeneralFunction
	{
		public PropertySetter (MemberHeaders headers, Identifier propName, Identifier argName, TypeName typeName, BlockStatement body)
			: base (headers, new FunctionDefinition (propName, new ArgumentDeclarations (new ArgumentDeclaration [] {new ArgumentDeclaration (argName, typeName, null)}), typeName, body))
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
		public ForEachStatement (ForEachIterator iter, Expression target, Statement body)
		{
			Iterator = iter;
			Target = target;
			Body = body;
		}
		
		public ForEachIterator Iterator { get; set; }
		public Expression Target { get; set; }
		public Statement Body { get; set; }
	}

	public partial class ThrowStatement : Statement
	{
		public ThrowStatement (Expression target)
		{
			Target = target;
		}

		public Expression Target { get; set; }
	}

	public partial class TryStatement : Statement
	{
		public TryStatement (BlockStatement tryBlock, CatchBlocks catchBlocks, BlockStatement finallyBlock)
		{
			TryBlock = tryBlock;
			CatchBlocks = catchBlocks;
			FinallyBlock = finallyBlock;
		}
		
		public BlockStatement TryBlock { get; set; }
		public CatchBlocks CatchBlocks { get; set; }
		public BlockStatement FinallyBlock { get; set; }
	}
	
	public partial class CatchBlock
	{
		public CatchBlock (TypedIdentifier nameAndType, BlockStatement block)
		{
			NameAndType = nameAndType;
			Block = block;
		}
		
		public TypedIdentifier NameAndType { get; set; }
		public BlockStatement Block { get; set; }
	}

	public partial class CalcAssignStatement : Statement, ICalcAssignStatement
	{
		public CalcAssignStatement (ILeftValue left, Expression right, string oper)
		{
			Left = left;
			Right = right;
			Operator = oper;
		}
		
		public ILeftValue Left { get; set; }
		public Expression Right { get; set; }
		public string Operator { get; set; }
	}

	public partial class AssignmentExpression : Expression
	{
		public AssignmentExpression (ILeftValue lvalue, Expression rvalue)
		{
			Left = lvalue;
			Right = rvalue;
		}

		public ILeftValue Left { get; set; }
		public Expression Right { get; set; }
	}

	/*
	public partial class ConstantExpression : Expression
	{
		public ConstantExpression (Literal value)
		{
			Value = value;
		}
		
		public Literal Value { get; set; }
	}

	public partial class ParenthezedExpression : Expression
	{
		public ParenthezedExpression (Expression content)
		{
		}
	}
	*/

	public partial class Literal : Expression
	{
		public Literal (object value)
		{
			Value = value;
		}

		public object Value { get; set; }
	}

	public partial class TypeName
	{
		public TypeName (string raw)
		{
			Raw = raw;
		}
		
		public string Raw { get; set; }
		
		public override string ToString ()
		{
			return Raw;
		}
	}
}
