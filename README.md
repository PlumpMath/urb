# ULISP
Stay for micro lisp. 
A minimal Unity-compatible lisp language inpired by lisp/ruby/forth.

####1. Intend
 - a thin-layer of code transformation between csharp and common lisp, with mix of ruby warmmy code style. 
 
####2. Core Features
 - lisp macros.
 - ruby clean syntax style.
 - remain csharp familiar keywords.
 - maximum .net Framework compatibility.
 - Unity editor intergration.

####3. Mechanism
   
   - Urb simply transform your language into equal csharp syntax rules, yes, I'm doing the code generation way to experiment the best sample code format. If things goes well, then I will replace with CodeDom or IL Emit (if necessary).

The work flow:

        urb language support > tokenizer > code transformation > csharp syntax > csharp compiler > unity

####4. Examples: file Simple.ul

		(load System)
		(using System.Collections.Generic)
		(attr :public :executable)
		(extends :Object)

		(define user "deulamco")
		(member im_a_member "Hello")
		
		(define (print::void line::string args::params-object[])
		    (Console.WriteLine line))
		        
		(define (print::void line::string)
		    (Console.WriteLine line))

		(define (test::void)
		    (var i 0)
		    (label Condition)
		    (+= i 1)
		    (Console.WriteLine i)
		    (if (and (< i 10) (< -1 i) 
		             (or true false))
		        (jump Condition))
		    (var result i)
		    (Console.WriteLine "Good bye {0} !" result))

		(test)
		(print user)

I was experiment with all language samples transformation that can be translated into the same C# source. Just to find a way to express my thought style the most into programming. So in the end, I borrow from them all the characteristic I like the most.

Certainly, this is still experiment.
 You will know when it's ready. 
 The world will know :)
 
 * If you have any interests in language design/creation, please join me at: https://gitter.im/language-creator/Lobby
