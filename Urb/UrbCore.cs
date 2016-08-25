using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Urb
{
	public class UrbCore
	{
		#region Collections
		// Stack 
		private Stack<object> mainStack = new Stack<object>();
		// Line Stack
		private Stack<object> lineStack = new Stack<object>();
		// function
		private Dictionary<string, Action<object>> functionMap = 
			new Dictionary<string, Action<object>>();
		// codeblock
		private Dictionary<string, Action> codeblockMap = 
			new Dictionary<string, Action>();
		#endregion

		#region Init 
		public UrbCore(){}

		// Create new codeblock.
		public void NewCodeBlock(string name, Action codeBlock){
			codeblockMap.Add (name, codeBlock);
		}

		// Create new function.
		public void NewFunction(string name, Action<object> codeBlock){
			functionMap.Add (name, codeBlock);
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
			// string " "
			@"(?<string>\"".*\"")|" +
			// pair of a:b
			@"(?<pair>[a-zA-Z0-9$_]+:[a-zA-Z0-9$_]+)|" +
		
			// @variable
			@"(?<instance_variable>\@[a-zA-Z0-9$_]+)|" +

			// float 1f 2.0f
			@"(?<float>[-+]?[0-9]*\.?[0-9]+f)|"+
			// integer 120
			@"(?<integer>[+-]?[0-9]+)|" +
			// operators
			@"(?<operator>[\+|-|\*|\/|\^|\+\=|\-\=])|" +
			// boolean
		    @"(?<boolean_operator>[\>|\<|\=|\==|\>=|\<=])|"+

			//special_form
			@"(?<special_form>def|end|class|require|if)|" +

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
		public void Parse(string source, bool isDebug = false){
			var token_list = new List<Token> ();
			var regex_pattern = new Regex (pattern);
			var matches = regex_pattern.Matches (source);

			foreach (Match match in matches){
				int i = 0;
				foreach (Group group in match.Groups){
					
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
							if(isDebug) 
								Console.WriteLine("{0} - {1}", group_name, match_value);
						}}
					i++;
				}
			}
			// We need to eat the token here with a Lexer !
			Lex(token_list);
		}

		private void Lex(List<Token> token_list){
			Console.WriteLine("Lexing..");
			// Where we process tokens
			var is_special = false;
			var is_being_nested = false;
			var is_new_line = false;
			var nest_level = 0;
			var acc = new List<Token>();
			var line = new List<Token>();

			var token_array = token_list.ToArray();
			Console.WriteLine("Token List Length: {0}", token_array.Length);

			for (int i = 0; i < token_array.Length; i++)
			{
				var token = token_array[i];
				//Console.WriteLine("Check: {0}", token.Name == "newline" ? "newline" : token.Value);
				/************* Here we lex *************
				 * line by line, separated by newline.
				 * 
				 * just keep some special form as def/end/class
				 * the rest is normal keyword.
				 * 
				 * special_form -> (f a b c) -> new scope ( because mostly they are new function or class ).
				 * keyword & literal -> just  be normal sentence (c b a f) -> (f a b c) can be reversed.
				 */
				switch (token.Name)
				{
					case "newline":
						// push to line stack.
						line = acc;
						lineStack.Push(line);
						// clear accumulation.
						acc = new List<Token>();
						is_new_line = true;
						foreach(var word in line)
						Console.Write("{0} ", word.Value);
						Console.WriteLine();
					break;
						
					case "special_form":
						is_special = true;
						/* *************************************************
						 *  We process special form here to build up things.
						 *  Then wait for result from those small  pieces.
						 * 
						 ***************************************************/
						SpecialFormProcess(token, ref is_being_nested, ref nest_level, ref line); 
					break;
					
					default:
						is_special = false;
						// accumulate them.
						acc.Add(token);
						//Console.Write("{0} ", token.Value);
					break;
				}
			}
		}

		private void SpecialFormProcess(Token token, ref bool is_being_nested, ref int nest_level, ref List<Token> line)
		{
			switch (token.Value)
			{
				case "def":
					// Build new function block.
					is_being_nested = true;
					nest_level++;
					Console.Write("_def {0}_ ", nest_level);
					break;

				case "class":
					// Build new class block.
					is_being_nested = true;
					nest_level++;
					Console.Write("_class {0}_ ", nest_level);
					break;

				case "if":
					is_being_nested = true;
					nest_level++;
					Console.Write("_if {0}_ ", nest_level);
					break;

				case "require":
					is_being_nested = false;
					Console.Write("_require {0}_ ", nest_level);
					break;

				case "end":
					// end every block.
					is_being_nested = false;
					if (nest_level <= 0) throw new Exception("lacking special form");
					else nest_level--;
					Console.WriteLine("__end {0}__\n", nest_level);
					break;
			}
			//Console.WriteLine("Nested Level: {0}", nest_level);
		}

		// Init all primatives.
		public void InitImage(){
			NewFunction ("require", (object o) => {
				
			});
		}
	}
}

