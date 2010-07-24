using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Irony.Parsing;

using MemberHeader = System.String;
using MemberHeaders = System.Collections.Generic.List<string>;
using TypeName = System.String;
using Identifier = System.String;
using QualifiedReference = System.String;

namespace FreeActionScript
{
	static class IronyExtensions
	{
		public static string ConcatToString (this int [] array, string sep)
		{
			string s = null;
			for (int i = 0; i < array.Length - 1; i++)
				s += i + sep;
			s += array [array.Length - 1];
			return s;
		}

		public static NonTerminal Name (this NonTerminal nt, string name)
		{
			nt.Name = name;
			return nt;
		}
		
		public static T Get<T> (this ParseTreeNode node, int index)
		{
			var x = node.ChildNodes [index].AstNode;
			if (x == null)
				throw new InvalidOperationException (String.Format ("Node {0} child {1} has null AstNode", node.Term.Name, index));
			return (T) x;
		}

		public static T GetNullable<T> (this ParseTreeNode node, int index)
		{
			return (T) node.ChildNodes [index].AstNode;
		}
	}

	[Language ("ActionScript", "3.0-", "ActionScript pseudo grammar")]
	public partial class ActionScriptGrammar : Grammar
	{
		NonTerminal DefaultNonTerminal (string label)
		{
			var nt = new NonTerminal (label);
			nt.AstNodeCreator = not_implemented;
			return nt;
		}

		NonTerminal BinaryOperatorTerminal (string label)
		{
			var nt = new NonTerminal (label);
			nt.AstNodeCreator = create_ast_binary_operator;
			return nt;
		}

		KeyTerm Keyword (string label)
		{
			return ToTerm (label);
		}

		Terminal ToRawTerm (string label)
		{
			var t = ToTerm (label);
			t.AstNodeCreator = delegate (ParsingContext ctx, ParseTreeNode node) {
				node.AstNode = label;
			};
			return t;
		}

		public ActionScriptGrammar ()
		{
CommentTerminal single_line_comment = new CommentTerminal ("SingleLineComment", "//", "\r", "\n");
CommentTerminal delimited_comment = new CommentTerminal ("DelimitedComment", "/*", "*/");

NonGrammarTerminals.Add (single_line_comment);
NonGrammarTerminals.Add (delimited_comment);

// FIXME: should be generic identifiers or its own.
IdentifierTerminal identifier = TerminalFactory.CreateCSharpIdentifier ("Identifier");
identifier.AllFirstChars += "$";
StringLiteral string_literal = TerminalFactory.CreateCSharpString ("StringLiteral");
StringLiteral char_literal = new StringLiteral ("CharLiteral", "'");
NumberLiteral numeric_literal = TerminalFactory.CreateCSharpNumber ("Number");
RegExLiteral regex_literal = new RegExLiteral ("RegEx");
regex_literal.Switches.Add ('s', RegexOptions.None); // FIXME: other than None

// keywords
KeyTerm keyword_package = Keyword ("package");
KeyTerm keyword_import = Keyword ("import");
KeyTerm keyword_use = Keyword ("use");
KeyTerm keyword_namespace = Keyword ("namespace");
KeyTerm keyword_class = Keyword ("class");
KeyTerm keyword_extends = Keyword ("extends");
KeyTerm keyword_public = Keyword ("public");
KeyTerm keyword_internal = Keyword ("internal");
KeyTerm keyword_private = Keyword ("private");
KeyTerm keyword_static = Keyword ("static");
KeyTerm keyword_dynamic = Keyword ("dynamic");
KeyTerm keyword_override = Keyword ("override");
KeyTerm keyword_const = Keyword ("const");
KeyTerm keyword_var = Keyword ("var");
KeyTerm keyword_function = Keyword ("function");
KeyTerm keyword_get = Keyword ("get");
KeyTerm keyword_set = Keyword ("set");
KeyTerm keyword_throw = Keyword ("throw");
KeyTerm keyword_try = Keyword ("try");
KeyTerm keyword_catch = Keyword ("catch");
KeyTerm keyword_finally = Keyword ("finally");
KeyTerm keyword_return = Keyword ("return");
KeyTerm keyword_if = Keyword ("if");
KeyTerm keyword_else = Keyword ("else");
KeyTerm keyword_switch = Keyword ("switch");
KeyTerm keyword_case = Keyword ("case");
KeyTerm keyword_default = Keyword ("default");
KeyTerm keyword_while = Keyword ("while");
KeyTerm keyword_do = Keyword ("do");
KeyTerm keyword_for = Keyword ("for");
KeyTerm keyword_each = Keyword ("each");
KeyTerm keyword_in = Keyword ("in");
KeyTerm keyword_break = Keyword ("break");
KeyTerm keyword_continue = Keyword ("continue");
KeyTerm keyword_null = Keyword ("null");
KeyTerm keyword_new = Keyword ("new");
KeyTerm keyword_delete = Keyword ("delete");
KeyTerm keyword_is = Keyword ("is");
KeyTerm keyword_as = Keyword ("as");


var compile_unit = DefaultNonTerminal ("compile_unit");
var compile_unit_item = DefaultNonTerminal ("compile_unit_item");
var package_decl = DefaultNonTerminal ("package_declaration");
var package_name = DefaultNonTerminal ("package_name");
var package_contents = DefaultNonTerminal ("package_contents");
var package_content = DefaultNonTerminal ("package_content");
var namespace_or_class = DefaultNonTerminal ("namespace_or_class");
var namespace_or_class_headless = DefaultNonTerminal ("namespace_or_class_headless");
var namespace_decl = DefaultNonTerminal ("namespace_declaration");
var import = DefaultNonTerminal ("import");
var namespace_uses = DefaultNonTerminal ("namespace_uses");
var namespace_use = DefaultNonTerminal ("namespace_use");
var type_name_wild = DefaultNonTerminal ("type_name_wild");
var semi_opt = DefaultNonTerminal ("semicolon_optional");
var class_decl = DefaultNonTerminal ("class_declaration");
var event_decls = DefaultNonTerminal ("event_declarations");
var event_decl = DefaultNonTerminal ("event_declaration");
var event_decl_members = DefaultNonTerminal ("event_decl_members");
var event_decl_member = DefaultNonTerminal ("event_decl_member");
var access_modifier = DefaultNonTerminal ("access_modifier");
var class_members = DefaultNonTerminal ("class_members");
var class_member = DefaultNonTerminal ("class_member");
var member_header = DefaultNonTerminal ("member_header");
var constant_declaration = DefaultNonTerminal ("constant_declaration");
var field_declaration = DefaultNonTerminal ("field_declaration");
var property_function = DefaultNonTerminal ("property_function");
var property_getter = DefaultNonTerminal ("property_getter");
var property_setter = DefaultNonTerminal ("property_setter");
var general_function = DefaultNonTerminal ("general_function");
var general_function_headless = DefaultNonTerminal ("general_function_headless");
var function_nameless = DefaultNonTerminal ("function_nameless");
var constructor = DefaultNonTerminal ("constructor");
var argument_decls = DefaultNonTerminal ("argument_declarations");
var varargs_decl = DefaultNonTerminal ("varargs_decl");
var named_argument_decls = DefaultNonTerminal ("named_argument_declarations");
var argument_decl = DefaultNonTerminal ("argument_declaration");
var argument_type = DefaultNonTerminal ("argument_type");
var qualified_reference = DefaultNonTerminal ("qualified_reference");
var type_name = qualified_reference;//DefaultNonTerminal ("type_name");
var member_reference = DefaultNonTerminal ("member_reference");
var assignment_opt = DefaultNonTerminal ("assignment_opt");
var lvalue = DefaultNonTerminal ("lvalue");
var statements = DefaultNonTerminal ("statements");
var statement = DefaultNonTerminal ("statement");
var statement_lacking_colon_then_colon = DefaultNonTerminal ("statement_lacking_colon_then_colon");
var statement_lacking_colon = DefaultNonTerminal ("statement_lacking_colon");

var local_function_statement = DefaultNonTerminal ("local_function_statement");
var assign_statement = DefaultNonTerminal ("assign_statement");
var calc_assign_statement = DefaultNonTerminal ("calc_assign_statement");
var return_statement = DefaultNonTerminal ("return_statement");
var function_call_statement = DefaultNonTerminal ("function_call_statement");
var call_arguments = DefaultNonTerminal ("call_arguments");
var call_argument = DefaultNonTerminal ("call_argument");
var delete_statement = DefaultNonTerminal ("delete_statement");
var local_var_decl_statement = DefaultNonTerminal ("local_var_decl_statement");
var name_value_pairs = DefaultNonTerminal ("name_value_pairs");
var name_value_pair = DefaultNonTerminal ("name_value_pair");
var if_statement = DefaultNonTerminal ("if_statement");
var else_block = DefaultNonTerminal ("else_block");
var switch_statement = DefaultNonTerminal ("switch_statement");
var switch_cond_blocks = DefaultNonTerminal ("switch_conditional_blocks");
var switch_cond_block = DefaultNonTerminal ("switch_cond_block");
var condition_label = DefaultNonTerminal ("condition_label");
var while_statement = DefaultNonTerminal ("while_statement");
var do_while_statement = DefaultNonTerminal ("do_while_statement");
var for_statement = DefaultNonTerminal ("for_statement");
var for_statement_remaining = DefaultNonTerminal ("for_statement_remaining");
var for_c_style_statement = DefaultNonTerminal ("for_c_style_statement");
var for_in_statement = DefaultNonTerminal ("for_in_statement");
var for_initializers = DefaultNonTerminal ("for_initializers");
var for_assign_statements = DefaultNonTerminal ("for_assign_statements");
var for_iterators = DefaultNonTerminal ("for_iterators");
var for_iterator = DefaultNonTerminal ("for_iterator");
var for_each_statement = DefaultNonTerminal ("for_each_statement");
var for_each_iterator = DefaultNonTerminal ("for_each_iterator");
var break_statement = DefaultNonTerminal ("break_statement");
var continue_statement = DefaultNonTerminal ("continue_statement");
var throw_statement = DefaultNonTerminal ("throw_statement");
var try_statement = DefaultNonTerminal ("try_statement");
var catch_blocks = DefaultNonTerminal ("catch_blocks");
var catch_block = DefaultNonTerminal ("catch_block");
var exception_type_part = DefaultNonTerminal ("exception_type_part");
var finally_block = DefaultNonTerminal ("finally_block");
var block_statement = DefaultNonTerminal ("block_statement");

var expression = DefaultNonTerminal ("expression");
var assignment_expression = DefaultNonTerminal ("assignment_expression");
var conditional_expression = DefaultNonTerminal ("conditional_expression");
var or_expression = BinaryOperatorTerminal ("or_expression");
var and_expression = BinaryOperatorTerminal ("and_expression");
var equality_expression = BinaryOperatorTerminal ("equality_expression");
var relational_expression = BinaryOperatorTerminal ("relational_expression");
var additive_expression = BinaryOperatorTerminal ("additive_expression");
var multiplicative_expression = BinaryOperatorTerminal ("multiplicative_expression");
var shift_expression = BinaryOperatorTerminal ("shift_expression");
var unary_expression = DefaultNonTerminal ("unary_expression");
var unary_operator = DefaultNonTerminal ("unary_operator");
var inc_dec_expression = DefaultNonTerminal ("inc_dec_expression");
var union_expression = BinaryOperatorTerminal ("union_expression");
var union_operator = BinaryOperatorTerminal ("union_operator");
var iteration_expression = DefaultNonTerminal ("iteration_expression");
var array_access_expression = DefaultNonTerminal ("array_access_expression");
var primary_expression = DefaultNonTerminal ("primary_expression");
var function_call_expression = DefaultNonTerminal ("function_call_expression");
var member_reference_expression = DefaultNonTerminal ("member_reference_expression");
var new_object_expression = DefaultNonTerminal ("new_object_expression");
var literal_array_expression = DefaultNonTerminal ("literal_array_expression");
var array_items = DefaultNonTerminal ("array_items");
var literal_hash_expression = DefaultNonTerminal ("literal_hash_expression");
var hash_items = DefaultNonTerminal ("hash_items");
var hash_item = DefaultNonTerminal ("hash_item");
var identifier_or_literal = DefaultNonTerminal ("identifier_or_literal");
var as_expression = DefaultNonTerminal ("as_expression");
var embedded_function_expression = DefaultNonTerminal ("embedded_function_expression");
var literal = DefaultNonTerminal ("literal");

// <construction_rules>

// non-terminals

compile_unit.Rule = MakeStarRule (compile_unit, null, compile_unit_item);
compile_unit_item.Rule = package_decl | import | class_decl;
package_decl.Rule = keyword_package + package_name + "{" + package_contents + "}";
package_name.Rule = qualified_reference;
package_contents.Rule = MakeStarRule (package_contents, null, package_content);
package_content.Rule = import | namespace_or_class;
// It is wrong if event_decls are placed before namespace decl, having them before namespace_decl and class_decl results in shift-reduce conflict, and there is no good way to resolve this extra event_decls issue.
namespace_or_class.Rule = event_decls + member_header + namespace_or_class_headless;
namespace_or_class_headless.Rule = namespace_decl | class_decl;
namespace_decl.Rule = keyword_namespace + identifier + ";";

import.Rule = keyword_import + type_name_wild + ";";
namespace_uses.Rule = MakeStarRule (namespace_uses, null, namespace_use);
namespace_use.Rule = keyword_use + keyword_namespace + identifier + ";";

class_decl.Rule = keyword_class + identifier + (Empty | keyword_extends + type_name) + "{" + namespace_uses + class_members + "}";
event_decls.Rule = MakeStarRule (event_decls, null, event_decl);
event_decl.Rule = "[" + type_name + "(" + event_decl_members + ")" + "]"; // type_name must be "Event"
event_decl_members.Rule = MakeStarRule (event_decl_members, ToTerm (","), event_decl_member);
event_decl_member.Rule = identifier + "=" + literal;

// class member
access_modifier.Rule = keyword_public | keyword_internal | keyword_private | identifier | keyword_static | keyword_dynamic | keyword_override;
class_members.Rule = MakeStarRule (class_members, null, class_member);
class_member.Rule = constant_declaration | field_declaration | property_function | general_function | constructor ;

member_header.Rule = MakeStarRule (member_header, null, access_modifier);

// field and constant
constant_declaration.Rule = member_header + keyword_const + name_value_pairs + semi_opt;
field_declaration.Rule = member_header + keyword_var + name_value_pairs + semi_opt;
assignment_opt.Rule = Empty | "=" + expression;

// functions
property_function.Rule = property_getter | property_setter;
property_getter.Rule = member_header + keyword_function + keyword_get + identifier + "(" + ")" + ":" + type_name + block_statement;
property_setter.Rule = member_header + keyword_function + keyword_set + identifier + "(" + identifier + ":" + type_name + ")" + ":" + "void" + block_statement;
general_function.Rule = member_header + general_function_headless;
general_function_headless.Rule = keyword_function + identifier + function_nameless;
function_nameless.Rule = "(" + argument_decls + ")" + (Empty | ":" + type_name_wild) + block_statement;
constructor.Rule = keyword_function + identifier + "(" + argument_decls + ")" + block_statement;
argument_decls.Rule = MakeStarRule (named_argument_decls, ToTerm (","), argument_decl);
argument_decl.Rule = // FIXME: there is an ambiguation issue; on foo.<bar>=baz ">=" conflicts with comparison operator.
	identifier + ":" + argument_type + assignment_opt
	| varargs_decl
	;
varargs_decl.Rule = "..." + identifier;
argument_type.Rule = type_name_wild;

// statements
statements.Rule = MakeStarRule (statements, null, statement);
statement.Rule =
	statement_lacking_colon_then_colon
	| if_statement
	| switch_statement
	| while_statement
	| do_while_statement
	| for_statement
	| for_each_statement
	| block_statement
	| try_statement
	| local_function_statement
	;

local_function_statement.Rule = general_function_headless;

statement_lacking_colon_then_colon.Rule = statement_lacking_colon + ";";

statement_lacking_colon.Rule =
	assign_statement
	| calc_assign_statement
	| return_statement
	| function_call_statement
	| delete_statement
	| local_var_decl_statement
	| break_statement
	| continue_statement
	| throw_statement
	;

assign_statement.Rule = assignment_expression;
calc_assign_statement.Rule =
	inc_dec_expression
	| lvalue + "+=" + expression
	| lvalue + "-=" + expression
	| lvalue + "*=" + expression
	| lvalue + "/=" + expression
	| lvalue + "%=" + expression
	| lvalue + "&=" + expression
	| lvalue + "|=" + expression
	| lvalue + "<<=" + expression
	| lvalue + ">>=" + expression
	;
return_statement.Rule =
	keyword_return
	| keyword_return + expression;
function_call_statement.Rule = function_call_expression;
if_statement.Rule =
	keyword_if + "(" + expression + ")" + statement + else_block;
else_block.Rule = Empty | PreferShiftHere () + keyword_else + statement;
switch_statement.Rule =
	keyword_switch + "(" + expression + ")" + "{" + switch_cond_blocks + "}";
switch_cond_blocks.Rule = MakeStarRule (switch_cond_blocks, null, switch_cond_block);
switch_cond_block.Rule = condition_label + ":" + statements;
condition_label.Rule =
	keyword_case + literal
	| keyword_case + qualified_reference // identifier, or constant
	| keyword_default;
while_statement.Rule = keyword_while + "(" + expression + ")" + statement;
do_while_statement.Rule = keyword_do + statement + keyword_while + "(" + expression + ")" + ";";
for_statement.Rule = keyword_for + "(" + for_statement_remaining;
for_statement_remaining.Rule = for_c_style_statement | for_in_statement;
for_c_style_statement.Rule = for_initializers + ";" + expression + ";" + for_iterators + ")" + statement;
for_in_statement.Rule = for_each_iterator + keyword_in + expression + ")" + statement;
for_initializers.Rule = local_var_decl_statement | for_assign_statements;
for_assign_statements.Rule = MakeStarRule (for_assign_statements, ToTerm (","), assign_statement);
for_iterators.Rule = MakeStarRule (for_iterators, ToTerm (","), for_iterator);
for_iterator.Rule = assign_statement | calc_assign_statement;
for_each_statement.Rule = keyword_for + keyword_each + "(" + for_each_iterator + keyword_in + expression + ")" + statement;
for_each_iterator.Rule = identifier | keyword_var + identifier + ":" + argument_type;
break_statement.Rule = keyword_break;
continue_statement.Rule = keyword_continue;
throw_statement.Rule = keyword_throw + expression;
try_statement.Rule = keyword_try + block_statement + catch_blocks + finally_block;
catch_blocks.Rule = MakeStarRule (catch_blocks, null, catch_block);
catch_block.Rule = keyword_catch + exception_type_part + block_statement;
exception_type_part.Rule = Empty | ToTerm ("(") + identifier + ":" + type_name + ")";
finally_block.Rule = Empty | keyword_finally + block_statement;
block_statement.Rule = ToTerm ("{") + statements + "}";
local_var_decl_statement.Rule = keyword_var + name_value_pairs;
name_value_pairs.Rule = MakePlusRule (name_value_pairs, ToTerm (","), name_value_pair);
name_value_pair.Rule = identifier + ":" + argument_type + assignment_opt;
delete_statement.Rule = keyword_delete + expression;


// expressions
expression.Rule =
	conditional_expression
	| assignment_expression;

assignment_expression.Rule = lvalue + "=" + expression;

conditional_expression.Rule =
	or_expression
	| or_expression + "?" + conditional_expression + ":" + conditional_expression;
or_expression.Rule =
	and_expression
	| or_expression + "||" + and_expression;
and_expression.Rule =
	equality_expression 
	| and_expression + "&&" + equality_expression;
equality_expression.Rule =
	relational_expression
	| equality_expression + "===" + relational_expression
	| equality_expression + "!==" + relational_expression
	| equality_expression + "==" + relational_expression
	| equality_expression + "!=" + relational_expression
	| equality_expression + keyword_is + relational_expression;
relational_expression.Rule =
	additive_expression
	| relational_expression + "<" + additive_expression
	| relational_expression + "<=" + additive_expression
	| relational_expression + ">" + additive_expression
	| relational_expression + ">=" + additive_expression;
additive_expression.Rule =
	multiplicative_expression
	| additive_expression + "+" + multiplicative_expression
	| additive_expression + "-" + multiplicative_expression;
multiplicative_expression.Rule =
	as_expression
	| multiplicative_expression + "*" + as_expression
	| multiplicative_expression + "/" + as_expression
	| multiplicative_expression + "%" + as_expression;
as_expression.Rule =
	shift_expression
	| shift_expression + keyword_as + type_name;
shift_expression.Rule =
	union_expression
	| shift_expression + "<<<" + union_expression
	| shift_expression + "<<" + union_expression
	| shift_expression + ">>>" + union_expression
	| shift_expression + ">>" + union_expression;
union_expression.Rule =
	unary_expression
	| union_expression + union_operator + unary_expression;
union_operator.Rule = ToTerm ("&") | "|" | "^";
unary_expression.Rule =
	iteration_expression
	| inc_dec_expression
	| unary_operator + iteration_expression;
unary_operator.Rule = ToTerm ("-") | "!" | "~";
inc_dec_expression.Rule = 
	member_reference_expression + ToTerm ("++")
	| member_reference_expression + ToTerm ("--")
	| ToTerm ("++") + member_reference_expression
	| ToTerm ("--") + member_reference_expression;

iteration_expression.Rule = // weird, but "!x in y" is parsed as "!(x in y)"
	array_access_expression
	| literal_hash_expression
	| array_access_expression + keyword_in + array_access_expression;

array_access_expression.Rule =
	primary_expression
	| array_access_expression + "[" + expression + "]"
	;

primary_expression.Rule =
	member_reference_expression
	| function_call_expression
	| ToTerm ("(") + expression + ")"
	| new_object_expression
	| literal_array_expression
	| literal
	| embedded_function_expression;

lvalue.Rule =
	member_reference_expression
	;

literal.Rule = numeric_literal | string_literal | char_literal | regex_literal | keyword_null;

member_reference_expression.Rule =
	member_reference
	// This is required for lvalue.
	| member_reference_expression + "[" + expression + "]";

function_call_expression.Rule = member_reference_expression + "(" + call_arguments + ")";
call_arguments.Rule = MakeStarRule (call_arguments, ToTerm (","), call_argument);
call_argument.Rule = expression;
member_reference.Rule =
	identifier // FIXME: what should I do here? unify with qualified_reference? but some requires expression
	| primary_expression + ToRawTerm (".") + identifier
	| primary_expression + ToRawTerm ("::") + identifier
	| primary_expression + ToRawTerm (".<") + type_name + ToRawTerm (">")
	;

new_object_expression.Rule = keyword_new + type_name + "(" + call_arguments + ")";
literal_array_expression.Rule = ToTerm ("[") + array_items + "]";
array_items.Rule = MakeStarRule (array_items, ToTerm (","), expression);
literal_hash_expression.Rule = ToTerm ("{") + hash_items + "}";
hash_items.Rule = MakeStarRule (hash_items, ToTerm (","), hash_item);
hash_item.Rule = identifier_or_literal + ":" + expression;
identifier_or_literal.Rule = identifier | literal;
embedded_function_expression.Rule = keyword_function + function_nameless;

// this contains both type name (which includes generic) and member reference, but it's easier to treat them as identical.
qualified_reference.Rule =
	identifier
	| qualified_reference + "." + identifier
	| qualified_reference + ".<" + type_name + ">";

type_name_wild.Rule = qualified_reference | qualified_reference + ToRawTerm (".*") | ToRawTerm ("*");
//type_name.Rule = qualified_reference;
semi_opt.Rule = Empty | ";";

// </construction_rules>

Root = compile_unit;

// operators (copied from CSharpGrammar)

RegisterOperators (1, "||");
RegisterOperators (2, "&&");
RegisterOperators (3, "|");
RegisterOperators (5, "&");
RegisterOperators (6, "==", "!=");
RegisterOperators (7, "<", ">", "<=", ">=", "is", "as");
RegisterOperators (8, "<<", ">>");
RegisterOperators (9, "++", "--");
RegisterOperators (10, "+", "-");
RegisterOperators (11, "*", "/", "%");
RegisterOperators (12, ".");
RegisterOperators (-3, "=", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=");
RegisterOperators (-2, "?");
Delimiters = "{}[](),:;+-*/%&|^!~<>=";
// not sure if "." should be added here.
RegisterPunctuation (";", ",", "{", "}", "[", "]", ":", ".", "?");

MarkNotReported (keyword_package);
MarkTransient (compile_unit_item, package_name, package_content, namespace_or_class_headless, class_member, property_function, argument_type, statement, statement_lacking_colon, for_statement_remaining, for_iterator, expression, call_argument, lvalue, union_operator, unary_operator, identifier_or_literal);
//MarkTransient (compile_unit_item, package_decl, package_content, class_member, statement, statement_lacking_colon, expression, conditional_expression, iteration_expression, or_expression, and_expression, equality_expression, relational_expression, additive_expression, multiplicative_expression, as_expression, shift_expression, union_expression, unary_expression, new_object_expression, general_function_headless, function_nameless, call_argument);


		identifier.AstNodeCreator = delegate (ParsingContext ctx, ParseTreeNode node) { node.AstNode = node.FindTokenAndGetText (); };
		string_literal.AstNodeCreator = delegate (ParsingContext ctx, ParseTreeNode node) { node.AstNode = node.FindTokenAndGetText (); };
		char_literal.AstNodeCreator = delegate (ParsingContext ctx, ParseTreeNode node) { node.AstNode = node.FindTokenAndGetText (); };
		numeric_literal.AstNodeCreator = delegate (ParsingContext ctx, ParseTreeNode node) { node.AstNode = node.FindTokenAndGetText (); };
		regex_literal.AstNodeCreator = delegate (ParsingContext ctx, ParseTreeNode node) { node.AstNode = node.FindTokenAndGetText (); };

		compile_unit.AstNodeCreator = create_ast_compile_unit;
		package_decl.AstNodeCreator = create_ast_package_decl;
		package_contents.AstNodeCreator = create_ast_simple_list<IPackageContent>;
namespace_or_class.AstNodeCreator = create_ast_namespace_or_class;
namespace_decl.AstNodeCreator = create_ast_namespace_decl;
import.AstNodeCreator = create_ast_import;
  namespace_uses.AstNodeCreator = create_ast_simple_list<NamespaceUse>;
namespace_use.AstNodeCreator = create_ast_namespace_use;
class_decl.AstNodeCreator = create_ast_class_decl;
  event_decls.AstNodeCreator = create_ast_simple_list<EventDeclaration>;
event_decl.AstNodeCreator = create_ast_event_decl;
  event_decl_members.AstNodeCreator = create_ast_simple_list<NameValuePair>;
event_decl_member.AstNodeCreator = create_ast_event_decl_member;
access_modifier.AstNodeCreator = create_ast_select_single_child;
  class_members.AstNodeCreator = create_ast_simple_list<IClassMember>;
member_header.AstNodeCreator = create_ast_simple_list<MemberHeader>;
constant_declaration.AstNodeCreator = create_ast_constant_declaration;
field_declaration.AstNodeCreator = create_ast_field_declaration;
assignment_opt.AstNodeCreator = create_ast_assignment_opt;
property_getter.AstNodeCreator = create_ast_property_getter;
property_setter.AstNodeCreator = create_ast_property_setter;
general_function.AstNodeCreator = create_ast_general_function;
general_function_headless.AstNodeCreator = create_ast_general_function_headless;
function_nameless.AstNodeCreator = create_ast_function_nameless;
constructor.AstNodeCreator = create_ast_constructor;
  argument_decls.AstNodeCreator = create_ast_simple_list<ArgumentDeclaration>;
argument_decl.AstNodeCreator = create_ast_argument_decl;
varargs_decl.AstNodeCreator = create_ast_varargs_decl;
qualified_reference.AstNodeCreator = create_ast_qualified_reference;
  statements.AstNodeCreator = create_ast_simple_list<Statement>;
local_function_statement.AstNodeCreator = create_ast_local_function_statement;
statement_lacking_colon_then_colon.AstNodeCreator = create_ast_statement_lacking_colon_then_colon;
assign_statement.AstNodeCreator = create_ast_assign_statement;
calc_assign_statement.AstNodeCreator = create_ast_calc_assign_statement;
return_statement.AstNodeCreator = create_ast_return_statement;
function_call_statement.AstNodeCreator = create_ast_expression_statement;
if_statement.AstNodeCreator = create_ast_if_statement;
else_block.AstNodeCreator = create_ast_else_block;
switch_statement.AstNodeCreator = create_ast_switch_statement;
  switch_cond_blocks.AstNodeCreator = create_ast_simple_list<SwitchBlock>;
switch_cond_block.AstNodeCreator = create_ast_switch_cond_block;
condition_label.AstNodeCreator = create_ast_condition_label;
while_statement.AstNodeCreator = create_ast_while_statement;
do_while_statement.AstNodeCreator = create_ast_do_while_statement;
for_statement.AstNodeCreator = create_ast_for_statement;
for_c_style_statement.AstNodeCreator = create_ast_for_c_style_statement;
for_in_statement.AstNodeCreator = create_ast_for_in_statement;
for_initializers.AstNodeCreator = create_ast_for_initializers;
  for_assign_statements.AstNodeCreator = create_ast_simple_list<AssignmentExpressionStatement>;
  for_iterators.AstNodeCreator = create_ast_simple_list<IForIterator>;
for_each_statement.AstNodeCreator = create_ast_for_each_statement;
for_each_iterator.AstNodeCreator = create_ast_for_each_iterator;
break_statement.AstNodeCreator = create_ast_break_statement;
continue_statement.AstNodeCreator = create_ast_continue_statement;
throw_statement.AstNodeCreator = create_ast_throw_statement;
try_statement.AstNodeCreator = create_ast_try_statement;
  catch_blocks.AstNodeCreator = create_ast_simple_list<CatchBlock>;
catch_block.AstNodeCreator = create_ast_catch_block;
exception_type_part.AstNodeCreator = create_ast_exception_type_part;
finally_block.AstNodeCreator = create_ast_finally_block;
block_statement.AstNodeCreator = create_ast_block_statement;
local_var_decl_statement.AstNodeCreator = create_ast_local_var_decl_statement;
  name_value_pairs.AstNodeCreator = create_ast_simple_list<NameValuePair>;
name_value_pair.AstNodeCreator = create_ast_name_value_pair;
delete_statement.AstNodeCreator = create_ast_delete_statement;
assignment_expression.AstNodeCreator = create_ast_assignment_expression;
conditional_expression.AstNodeCreator = create_ast_conditional_expression;
as_expression.AstNodeCreator = create_ast_as_expression;
unary_expression.AstNodeCreator = create_ast_unary_expression;
inc_dec_expression.AstNodeCreator = create_ast_inc_dec_expression;
iteration_expression.AstNodeCreator = create_ast_iteration_expression;
array_access_expression.AstNodeCreator = create_ast_array_access_expression;
literal.AstNodeCreator = create_ast_literal;
member_reference_expression.AstNodeCreator = create_ast_member_reference_expression;
member_reference.AstNodeCreator = create_ast_member_reference;
primary_expression.AstNodeCreator = create_ast_primary_expression;
function_call_expression.AstNodeCreator = create_ast_function_call_expression;
  call_arguments.AstNodeCreator = create_ast_simple_list<Expression>;
new_object_expression.AstNodeCreator = create_ast_new_object_expression;
literal_array_expression.AstNodeCreator = create_ast_literal_array_expression;
  array_items.AstNodeCreator = create_ast_simple_list<Expression>;
literal_hash_expression.AstNodeCreator = create_ast_literal_hash_expression;
  hash_items.AstNodeCreator = create_ast_simple_list<HashItem>;
hash_item.AstNodeCreator = create_ast_hash_item;
embedded_function_expression.AstNodeCreator = create_ast_embedded_function_expression;
type_name_wild.AstNodeCreator = create_ast_type_name_wild;
//type_name.AstNodeCreator = create_ast_simple_list<TypeName>;
semi_opt.AstNodeCreator = create_ast_semi_opt;
		}
	}
}
