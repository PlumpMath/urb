# ULISP
Stay for micro lisp. 
A minimal Unity-compatible lisp language inpired by lisp/ruby/forth/ML.

####1. Intend
 - a thin-layer of code transformation between csharp and common lisp, type-inference/pattern matching of ML, with mix of ruby warmmy code style. 
 
####2. Core Features
 - simple.
 - lisp macros.
 - type inference.
 - interactive + compiled.
 - ruby clean syntax style.
 - csharp familiar keywords.
 - .net Framework compatibility.
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
		
		(define (print line args::params-object[])
		    (Console.WriteLine line args))
	
		(define (factorial acc n)
                    (if (> n 1)
                        (return (factorial (* acc n) (- n 1)))
                        (return acc)))
			
		(print "{0}" (factorial 1 10))

for real working sample, look into \example. 
It's already able to be compiled and use under Unity as component. 
Though, it need more time to be ready for something serious.

Certainly, this is still experiment.
 You will know when it's ready. 
 The world will know :)
 
 * If you have any interests in language design/creation, please join me at: https://gitter.im/language-creator/Lobby
