using System;
using System.Text;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace Urb
{
	public class ULisp
	{
		#region Collections
		// function
		private Dictionary<string, Action<object>> functionMap =
			new Dictionary<string, Action<object>>();
		// codeblock
		private Dictionary<string, Action> codeblockMap =
			new Dictionary<string, Action>();
		#endregion

		#region Init 

		public ULisp()
		{
		}

		// Create new codeblock.
		public void NewCodeBlock(string name, Action codeBlock)
		{
			codeblockMap.Add(name, codeBlock);
		}

		// Create new function.
		public void NewFunction(string name, Action<object> codeBlock)
		{
			functionMap.Add(name, codeBlock);
		}

		#endregion

		#region Syntax Table
		/********************
		 *  Syntax Pattern. *
		 ********************/
		private const string pattern =
			// \n and \r
			@"(?<newline>\n\t|\n|\r|\r\n)|" +
			// \t
			@"(?<tab>\t)|" +
			// comma
			@"(?<separator>,|\(|\))|" +
			// (a)
			@"(?<parens>(\(.*\)))|" +
			// [block]
			@"(?<open_brace>\[)|" +
			@"(?<close_brace>\])|" +
			// string " "
			@"(?<string>\"".*\"")|" +
			// pair of a:b
			@"(?<pair>[a-zA-Z0-9$_]+:[a-zA-Z0-9$_]+)|" +

			// @instant_variable
			@"(?<instance_variable>\@[a-zA-Z0-9$_]+)|" +
			// $global_variable
			@"(?<global_variable>\$[a-zA-Z0-9$_]+)|" +

			// float 1f 2.0f
			@"(?<float>[-+]?[0-9]*\.?[0-9]+f)|" +
			// integer 120
			@"(?<integer>[+-]?[0-9]+)|" +
			// operators
			@"(?<operator>\+=|\-=|\=|\+|\-|\*|\/|\^)|" +
			// boolean
			@"(?<boolean_compare>[\>|\<|\==|\>=|\<=])|" +
			@"(?<boolean_condition>[\|\||\&\&])|" +

			//compiler_directive
			@"(?<compiler_directive>pop|jump)|" +

			//special_form
			@"(?<special_form>defun|end|class|import|require|if|new|with|do|and|or|var|progn)|" +

			// :Symbol
			@"(?<symbol>:[a-zA-Z0-9$_.]+)|" +
			// Label:
			@"(?<label>[a-zA-Z0-9$_]+\:)|" +
			// Literal
			@"(?<literal>[a-zA-Z0-9$_.]+)|" +

			// the rest.
			@"(?<invalid>[^\s]+)";
		#endregion

		#region Parser
		// Readline.
		public string ParseIntoCSharp(string source, bool isDebugTransform = false, bool isDebugGrammar = false)
		{
			var token_list = new List<Token>();
			var regex_pattern = new Regex(pattern);
			var matches = regex_pattern.Matches(source);

			foreach (Match match in matches)
			{
				int i = 0;
				foreach (Group group in match.Groups)
				{

					var match_value = group.Value;

					var success = group.Success;

					// ignore capture index 0 and 1 (general and WhiteSpace)
					if (success && i > 1)
					{
						var group_name = regex_pattern.GroupNameFromNumber(i);
						if (group_name != "tab")
						{
							token_list.Add(new Token(group_name, match_value));

							//if (groupName != "newline") 
							if (isDebugGrammar)
								Console.WriteLine("{0} - {1}", group_name, match_value);
						}
					}
					i++;
				}
			}
			// We need to eat the token here with a Lexer !
			return Lex(token_list, isDebugTransform);
		}
		#endregion

		#region Line Helpers 
		private List<List<Token>> blocks = new List<List<Token>>();
		private List<string> csharp_blocks = new List<string>();

		private void InspectLine(List<Token> line)
		{
			foreach (var word in line) Console.Write("{0} ", word);
			Console.WriteLine();
		}

		private string ViewLine(Token[] line)
		{
			var s = String.Empty;
			foreach (var word in line) s += String.Format("{0} ", word.Value);
			return s;
		}

		private void AddSource(string line)
		{
			csharp_blocks.Add(line);
		}

		private void _print(string line, params object[] args)
		{
			Console.Write(line, args);
		}

		#endregion

		#region Lexer
		private Token[] _token_array;
		private int _token_index = -1;

		// Well, I think we should play dirty :P 
		public string Lex(List<Token> token_list, bool isDebugTransform = false)
		{
			if (isDebugTransform) Console.WriteLine("Lexing..");

			/// Lexing Start... ///

			var acc = new List<Token>();
			_token_array = token_list.ToArray();
			if (isDebugTransform) Console.WriteLine("Token List Length: {0}", _token_array.Length);

			///////////////////////////////////////////
			///										///
			/// Eat up all tokens and processing... ///
			/// 									///
			///////////////////////////////////////////
			TransformTokens(_token_array, acc, isDebugTransform);


			/////////////////////////////////////////
			///							  		  ///
			/// Print transformed C# source code. ///
			/// 								  ///
			/////////////////////////////////////////
			if (isDebugTransform) Console.WriteLine("\n\n[Transformed C#] \n");
			var csharp_source = new StringBuilder();
			foreach (var line in csharp_blocks)
			{
				if (isDebugTransform)
					Console.WriteLine(line);
				csharp_source.Append(line);
				csharp_source.AppendLine();
			}
			return csharp_source.ToString();
		}
		#endregion

		#region Code Transformation

		private Token PeakNextToken(int steps = 0)
		{
			return _token_array[_token_index + steps];
		}

		private List<string> variables = new List<string>();
		private List<string> references = new List<string>();
		private void TransformTokens(Token[] tokens, List<Token> acc, bool isDebugTransform)
		{
			_print("\nBuilding expressions from {0} tokens...\n", tokens.Length);
			_transformerIndex = 0;
			_expressions = new List<Expression>();

			while (_transformerIndex < tokens.Length - 1)
			{
				// build expression: //
				var e = BuildExpression(tokens, _transformerIndex);
				// accumulate all expressions: //
				if (e != null) _expressions.Add(e);
			}

			// Here we have a full parsed tree ! //
		}

		private int _open = 0;
		private int _close = 0;
		private List<Expression> _expressions;

		private Expression BuildExpression(Token[] tokens, int index)
		{
			var acc = new List<object>();
			var i = index;
			_transformerIndex = index;
			while (i < tokens.Length)
			{
				switch (tokens[i].Value)
				{
					case ")":
						_close++;
						_print(" )");
						//_print(" end#{0} \n", _transformerIndex);
						_transformerIndex = i + 1;
						return new Expression(acc.ToArray());

					case "(":
						_open++;
						_print("\n{0}#(", _expressions.Count);
						var e = BuildExpression(tokens, i + 1);
						if (_open == _close) return e;
						// else just keep adding.. //
						acc.Add(e);
						i = _transformerIndex;

						continue;
					default:
						if (tokens[i].Name == "newline")
						{
							i++;
							continue;
						}
						// except special separator we eat all //
						acc.Add(tokens[i]);
						/************************************
						 * 									*
						 * Would we transform token here ?	*
						 * 									*
						 ************************************/
						_print(" {0}", tokens[i].Value);
						break;
				}
				// cached index. //
				_transformerIndex = i;
				i++;
			}
			if (_open != _close)
				throw new Exception(string.Format(
					"Not balanced parentheses at {0}!", _transformerIndex));
			// just a null expression. //
			return null;
		}

		private Expression BuildExpression(Expression expressions)
		{
			/***************************************
			 * 									   *
			 * Here we build tokenized expression. *
			 * 									   *
			 ***************************************/
			return null;
		}

		private int _transformerIndex = 0;
		private Stack<object> _newListStack = new Stack<object>();

		private interface ITransformable
		{
			string TransformIntoCSharp();
		}

		private class Expression : ITransformable
		{
			public object function;
			public object[] elements;

			public Expression(object[] args)
			{
				if (args.Length != 0)
				{
					elements = new object[args.Length - 1];
					function = args[0];
					// copying... //
					for (int i = 1; i < args.Length; i++)
					{
						elements[i - 1] = args[i];
					}
					// done ! //
				}
			}

			public override string ToString()
			{
				var acc = "";
				foreach (var obj in elements) acc += obj.ToString() + " ";
				return string.Format("({0} {1})", function.ToString(), acc);
			}

			public string TransformIntoCSharp()
			{
				throw new NotImplementedException();
			}
		}

		private class List
		{
			public List<object> elements;
			public List(object[] args)
			{
				foreach (var arg in args)
				{
					elements.Add(arg);
				}
			}
		}

		private string InspectTypeAssignment(Token[] line)
		{
			// normally, it's just after the '=' //
			for (int i = 2; i < line.Length; i++)
			{
				// inspecting.. //
				// AddSource(string.Format("{0}:{1}\n", i, line[i].Value));
				switch (line[i].Name)
				{
					case "integer": return "Int32";
					case "float": return "float";
					case "string": return "String";
					case "literal": throw new NotImplementedException(line[i].Name);
					case "special_form":
						switch (line[i].Value)
						{
							case "new":
								_is_needed_closing = true;
								string type = "";
								for (int j = i + 1; j < line.Length; j++)
								{
									type += line[j].Value;
								}
								return type;
							default: throw new NotImplementedException(line[i].Name);
						}
					default: throw new NotImplementedException(line[i].Name);
				}
			}
			throw new NotImplementedException(line[2].ToString());
		}
		private bool _is_with = false;
		private bool _is_needed_closing = false;
		private void StatementBuild(Token[] line)
		{
			var statement = "";
			var is_literal = false;
			var is_opened = false;
			var is_new = false;
			_is_needed_closing = false;
			foreach (var word in line)
			{
				switch (word.Name)
				{
					case "integer":
					case "float":
					case "string":
					case "literal":
						if (is_literal && !is_opened && !is_new && !_is_with)
						{
							statement += "(";
							is_opened = true;
						}
						statement += word.Value + " ";
						is_literal = true;
						break;
					case "instance_variable":
						statement += string.Format("private {0} {1} ", InspectTypeAssignment(line), word.Value.Substring(1));
						break;
					case "global_variable":
						statement += string.Format("public {0} {1} ", InspectTypeAssignment(line), word.Value.Substring(1));
						break;
					case "pair": break;
					case "separator":
						statement += word.Value;
						break;
					case "special_form": /// new / with
						switch (word.Value)
						{
							case "new":
								statement += word.Value + " ";
								is_new = true;
								break;
							case "with":
								_is_with = true;
								statement += "( ";
								break;
							default: throw new NotImplementedException();
						}
						break;
					case "operator":
					case "boolean_operator":
					case "boolean_compare":
						statement += word.Value + " ";
						break;
					case "parens":
						// Need parens converter here.
						statement += word.ToString();
						break;
					default:
						throw new NotImplementedException(
							string.Format("Undefined {0} at:\n{1}",
										  word.ToString(),
										  ViewLine(line)));
				}
			}
			if (!_is_with)
			{
				if (is_literal && is_opened) statement += ")";
				if (_is_needed_closing) statement += "()";
				statement += ";";
			}
			else if (!is_new)
			{
				if (_token_index < _token_array.Length)
				{
					/// We ignore newline.
					//Console.WriteLine("PeakNext:" + PeakNextToken(1).Name);
					if (PeakNextToken(1).Value != "end")
						statement += ",";
				}
			}
			AddSource(statement);
		}

		#endregion

		#region Compiling

		public void Compile(string urb_source, string fileName, bool isExe = false, bool isDebugTransform = false, bool isDebugGrammar = false)
		{
			Console.WriteLine("* Urb :: A Rubylike language compiler *");

			var cs_source = ParseIntoCSharp(urb_source, isDebugTransform, isDebugGrammar);
			_compile_csharp_source(cs_source, fileName, isExe);
		}

		private void _compile_csharp_source(string source, string fileName, bool isExe = false, bool isInMemory = false)
		{
			var compiler_parameter = new CompilerParameters();
			compiler_parameter.GenerateExecutable = isExe;
			compiler_parameter.OutputAssembly = fileName;
			compiler_parameter.GenerateInMemory = isInMemory;
			foreach (var name in references)
				compiler_parameter.ReferencedAssemblies.Add(name);

			var compiler = new CSharpCodeProvider();

			var result = compiler.CompileAssemblyFromSource(compiler_parameter, new string[] { source });
			if (result.Errors.Count > 0)
			{
				// Display compilation errors.
				Console.WriteLine("Errors building  into {0}",
					result.PathToAssembly);
				foreach (CompilerError ce in result.Errors)
				{
					Console.WriteLine("  {0}", ce.ToString());
					Console.WriteLine();
				}
			}
			else
			{
				Console.WriteLine("Source built into {0} successfully.",
				result.PathToAssembly);
			}
		}

		#endregion
	}
}
