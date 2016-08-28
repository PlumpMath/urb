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
			@"(?<operator>[\+|-|\*|\/|\^|\+\=|\-\=])|" +
			// boolean
			@"(?<boolean_compare>[\>|\<|\=|\==|\>=|\<=])|" +
			@"(?<boolean_condition>[\|\||\&\&])|" +

			//compiler_directive
			@"(?<compiler_directive>pop|jump)|" +

			//special_form
			@"(?<special_form>def|end|class|require|if|new)|" +

			// :Symbol
			@"(?<symbol>:[a-zA-Z0-9$_]+)|" +
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

						// Prefix
						PrefixForm(line);

						// Postfix
						switch (line[line.Length - 1].Name)
						{
							case "special_form":
								// we got "if" and "new" here.
								switch (line[line.Length - 1].Value)
								{
									case "new":
										if (postpond_stack.Count == 0)
										{
											// something here.
											foreach (var word in line)
											{
												Console.Write("{0} ", word);
											}
										}
										else {
											// something else here.
										}
										break;

									case "if":
										// conds here.
										break;

								}
								break;

							case "compiler_directive":
								// we got "pop" and "jump" here.
								break;

							case "operator":
								// all numberic ops
								break;
							case "boolean_compare":
								// all logic ops
								break;

							case "boolean_condition":
								// all cond combinator
								break;

							default:
								// the rest are literal statements.
								break;
						}

						//Statements


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

		private void PrefixForm(Token[] line)
		{
			switch (line[0].Name)
			{
				case "special_form":
					if (line[0].Value != "end")
						SpecialForm(line);
					else // close braces everywhere. 
						AddSource("}");
					break;

				case "label":
					AddSource(
						new CultureInfo("en-US").TextInfo.ToTitleCase
						(line[0].Value));
					break;

				case "open_brace":
					/************************************************
					 *  We make room for new block of lines here.
					 * 
					 ************************************************/

					break;
				case "close_brace":
					/************************************************
					*  Then transform it here -> (a, b, c); as a statement.
					*  then push to a postpond stack.
					************************************************/

					break;
				default:
					//Console.WriteLine("Not implemented {0}", line[0]);
					break;
			}


		}

		private void SpecialForm(Token[] line)
		{
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

				case "new":

					break;

				case "do":

					break;
			}
		}
	}
}

