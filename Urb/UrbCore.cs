using System;
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

			// (a)
			@"(?<parens>(\(.*\)))|" +
			// [block]
			@"(?<open_brace>\[)|" +
			@"(?<close_brace>\])|" +
			// string " "
			@"(?<string>\"".*\"")|" +
			// pair of a:b
			@"(?<pair>[a-zA-Z0-9$_]+:[a-zA-Z0-9$_]+)|" +

			// @variable
			@"(?<instance_variable>\@[a-zA-Z0-9$_]+)|" +

			// float 1f 2.0f
			@"(?<float>[-+]?[0-9]*\.?[0-9]+f)|" +
			// integer 120
			@"(?<integer>[+-]?[0-9]+)|" +
			// operators
			@"(?<operator>\+=|\-=|\+|\-|\*|\/|\^)|" +
			// boolean
			@"(?<boolean_compare>[\>|\<|\=|\==|\>=|\<=])|" +
			@"(?<boolean_condition>[\|\||\&\&])|" +

			//compiler_directive
			@"(?<compiler_directive>pop|jump)|" +

			//special_form
			@"(?<special_form>definition|end|class|require|if|new|with|do|and|or)|" +

			// :Symbol
			@"(?<symbol>:[a-zA-Z0-9$_.]+)|" +
			// Label:
			@"(?<label>[a-zA-Z0-9$_]+\:)|" +
			// Literal
			@"(?<literal>[a-zA-Z0-9$_.]+)|" +

			// the rest.
			@"(?<invalid>[^\s]+)";
		#endregion

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

		private List<List<Token>> blocks = new List<List<Token>>();
		private List<string> csharp_blocks = new List<string>();

		private void InspectLine(List<Token> line)
		{
			foreach (var word in line) Console.Write("{0} ", word);
			Console.WriteLine();
		}

		private void AddSource(string line)
		{
			csharp_blocks.Add(line);
		}

		private Stack<List<Token>> statement_stack = new Stack<List<Token>>();
		private Stack<string> postpond_stack = new Stack<string>();


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

		// Well, I think we should play dirty :P 
		private void Lex(List<Token> token_list)
		{
			Console.WriteLine("Lexing..");
			var acc = new List<Token>();

			var token_array = token_list.ToArray();
			Console.WriteLine("Token List Length: {0}", token_array.Length);


			for (int i = 0; i < token_array.Length; i++)
			{
				var token = token_array[i];

				switch (token.Name)
				{
					case "newline":
						if (acc.Count == 0) break;
						var line = acc.ToArray();
						blocks.Add(acc);

						// Prefix: but there should be only one processing part.
						SpecialForm(line, acc);

						// Clear Acc and new line.
						acc = new List<Token>();
						Console.WriteLine();
						break;

					default:
						// accumulate them.
						acc.Add(token);
						Console.Write("{0} ", token.Value);
						break;
				}
			}


			Console.WriteLine("\n\n[Transformed C#] \n");
			foreach (var line in csharp_blocks)
			{
				Console.WriteLine(line);
			}
		}

		private List<string> variables = new List<string>();

		private void SpecialForm(Token[] line, List<Token> acc)
		{
			#region Prefix first.
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
							AddSource(string.Format("class {0} {{", line[1].Value));
							break;
						// class A: B
						case 4:
							AddSource(string.Format("class {0} : {1} {{", line[1].Value, line[3].Value));
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
						"public {0} ( {1} ) {{", def_type_name, pairs_string));
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

				case "end": AddSource("}"); break;
				#endregion
				// Statement
				default:
					switch (line[0].Name)
					{
						case "label": AddSource(line[0].Value); break;

						case "instance_variable":
						case "literal":
							if (!variables.Contains(line[0].Value))
							{
								// we need to make parens converter -> somerthing<A,B> 
								variables.Add(line[0].Value);
							}
							StatementBuild(line);
							break;
						case "compiler_directive":
							switch (line[0].Value)
							{
								case "jump": AddSource(string.Format("goto {0}", line[1].Value)); break;
								default: break;
							}
							break;

						default:
							break;
					}
					break;
			}
		}

		private void StatementBuild(Token[] line)
		{
			var statement = "";
			var is_literal = false;
			var is_opened = false;
			foreach (var word in line)
			{
				switch (word.Name)
				{
					case "integer":
					case "float":
					case "string":
					case "literal":
						if (is_literal && !is_opened)
						{
							statement += "(";
							is_opened = true;
						}
						statement += word.Value + " ";
						is_literal = true;
						break;
					case "special_form": // new 
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
						statement += word.ToString();
						break;
				}
			}
			if (is_literal) statement += ")";
			statement += ";";
			AddSource(statement);
		}
	}
}

