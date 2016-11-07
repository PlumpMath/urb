﻿using System;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Urb
{
	public class UrbCore
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
		public UrbCore() { }

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
			@"(?<special_form>def\-|def\+|end|class|require|if|new|with|do|and|or|var)|" +

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
		public void Parse(string source, bool isDebug = false)
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
							if (isDebug)
								Console.WriteLine("{0} - {1}", group_name, match_value);
						}
					}
					i++;
				}
			}
			// We need to eat the token here with a Lexer !
			Lex(token_list);
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

		#endregion

		/* Note on new Update :
		 * 
		 * As I feel so hopeless on the old source code design based on Ruby and partly Forth,
		 * I decided to totally change it into something more interesting to me. And matter to
		 * strengthen the flow into a single minded source. I tried to reduce the fragment here.
		 * 
		 * - So everything now is on stack. 
		 * - "newline" act like a statement enforce. 
		 * - "{}" act as a single element for easy encapsulation.
		 * - "end" simply a transition. since we are still parsing code.
		 * - "label" is removed.
		 * 
		 * I will see if we can do this as dirty as possible, so we may got the result as fast
		 * and as simple as possible. When the prototype work on compiling 100% C# compatible code
		 * we can hope the rest will do just fine. 
		 * 
		 * In the same style, certainly. That's so important.
		 */

		#region Lexer
		private Token[] _token_array;
		private int _token_index = -1;

		// Well, I think we should play dirty :P 
		private void Lex(List<Token> token_list)
		{
			Console.WriteLine("Lexing..");

			/// Lexing Start... ///

			var acc = new List<Token>();
			_token_array = token_list.ToArray();
			Console.WriteLine("Token List Length: {0}", _token_array.Length);

			///////////////////////////////////////////
			///										///
			/// Eat up all tokens and processing... ///
			/// 									///
			///////////////////////////////////////////
			for (int i = 0; i < _token_array.Length; i++)
			{
				/// caching token position ///
				_token_index = i;

				var first_token = _token_array[i];

				switch (first_token.Name)
				{
					case "newline":
						if (acc.Count == 0) break;
						var line = acc.ToArray();
						blocks.Add(acc);

						//////////////////////////////////////////////////////
						/// 											   ///
						/// We build a peaker here to see,			   ///
						/// if there's something we need in the next line. ///
						/// Since it reach 'newline', index already to the ///
						/// next line. So we can use to peak what's next.  ///
						/// 											   ///
						//////////////////////////////////////////////////////

						// Prefix: the singleton processing part.
						TransformTokens(line, acc);

						// Clear Acc and new line.
						acc = new List<Token>();
						Console.WriteLine();
						break;

					default:
						// accumulate them.
						acc.Add(first_token);
						Console.Write("{0} ", first_token.Value);
						break;
				}
			}
			// Oh, we forgot last line:
			var last_line = acc.ToArray();
			blocks.Add(acc);
			TransformTokens(last_line, acc);
			acc = new List<Token>();


			/////////////////////////////////////////
			///							  		  ///
			/// Print transformed C# source code. ///
			/// 								  ///
			/////////////////////////////////////////
			Console.WriteLine("\n\n[Transformed C#] \n");
			foreach (var line in csharp_blocks)
			{
				Console.WriteLine(line);
			}
		}
		#endregion

		private Token PeakNextToken(int steps = 0)
		{
			return _token_array[_token_index + steps];
		}

		private List<string> variables = new List<string>();

		private void TransformTokens(Token[] line, List<Token> acc)
		{
			#region Prefix keyword first.
			if (line.Length == 0) return;
			switch (line[0].Value)
			{
				case "require":
					// require a => Using a;
					AddSource(string.Format("using {0};", line[1].Value));
					break;
				case "class":
					//InspectLine(acc);
					switch (line.Length)
					{
						case 2: // class A {} 
							AddSource(string.Format("\nclass {0} {{", line[1].Value));
							break;
						// class A: B
						case 4:
							AddSource(string.Format("\nclass {0} : {1} {{", line[1].Value, line[3].Value));
							break;

						default:
							// leave multi inheritance later.
							break;
					}
					break;
				case "def":
					Console.WriteLine();
					// def a:void b:float c:int
					// but first we need to reverse all of them a:void -> void a
					var pairs = new List<string>();
					foreach (var word in line)
					{
						if (word.Name == "pair")
						{
							var pair = word.Value.Split(new char[] { ':' });
							var new_pair = pair[1] + " " + pair[0];
							pairs.Add(new_pair);
							Console.Write("{0} ", new_pair);
						}
					}
					// well now we got all pairs ! let add (, , , ) { 
					var def_type_name = pairs[0]; // public void something_i_give_up_on_you
												  // now for the rest of them with commas:
					var pairs_string = String.Empty;
					if (pairs.Count > 1)
						for (int j = 1; j < pairs.Count; j++)
						{
							pairs_string += pairs[j];
							if (j != pairs.Count - 1) pairs_string += ", ";
						}
					// now into braces (){
					AddSource(string.Format(
						"\npublic {0} ( {1} ) {{", def_type_name, pairs_string));
					break;

				case "if":
					// wondering if:
					var if_statement = "if (";
					var is_literal = false;
					foreach (var word in line)
					{
						if (word.Value != "if")
							switch (word.Value)
							{
								case "and": if_statement += "&& "; break;
								case "or": if_statement += "|| "; break;
								default: // Just literals
									if (is_literal && word.Name == "literal")
									{
										// double literal mean ( )
										if_statement += string.Format("({0})", word.Value);
									}
									else {
										if_statement += string.Format("{0} ", word.Value);
									}
									// memorized.
									is_literal = word.Name == "literal";

									break;

							}
					}
					if_statement += "){";
					AddSource(if_statement);
					break;

				case "var":
					AddSource(ViewLine(line) + ";");
					break;

				case "end":
					if (_is_with)
					{
						_is_with = false;
						AddSource(" );");
					}
					else
					{
						AddSource("}");
					}
					break;
				#endregion
				// Statement
				default:
					switch (line[0].Name)
					{
						case "label": AddSource(line[0].Value); break;
						///////////////////////////////////////
						/// 								///
						/// Here come the part of @variable ///
						/// 								///
						///////////////////////////////////////
						case "global_variable":
						case "instance_variable":
						case "literal":
							if (!variables.Contains(line[0].Value))
							{
								// we need to make parens converter -> somerthing<A,B> 
								variables.Add(line[0].Value);
								Console.WriteLine("variable: {0}", line[0].Value);
							}
							StatementBuild(line);
							break;
						case "compiler_directive":
							switch (line[0].Value)
							{
								case "jump": AddSource(string.Format("goto {0};", line[1].Value)); break;
								default: break;
							}
							break;

						default: throw new NotImplementedException(line[0].Name);
							//break;
					}
					break;
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
					Console.WriteLine("PeakNext:" + PeakNextToken(1).Name);
					if (PeakNextToken(1).Value != "end")
						statement += ",";
				}
			}
			AddSource(statement);
		}
	}
}

