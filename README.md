# Urb
A minimal Unity-compatible language inpired by lisp/ruby.

####1. Intend
 - a thin-layer of code transformation between csharp and common lisp, with mix of ruby warmmy code style. 
 
####2. Core Features
 - remain csharp familiar rules/keywords.
 - common Lisp features/syntax and macros.
 - maximum csharp/.net Framework compatibility.
 - Unity editor intergration.
 - Code Intellisense MonoDevelop plugin. 

####3. Mechanism
   
   - Urb simply transform your language into equal csharp syntax rule. 
   - It currently can work totally with 2 samples of Ruby/Lisp-like.

The work flow:

        urb language support > tokenizer > code transformation > csharp syntax > csharp compiler > unity

####4. Examples

  - Ruby-like syntax:

        import System
        import UnityEngine
        require System.Collections.Generic

        class Player < MonoBehaviour

          $Name = "deulamco"
          @dict = new Dictionary <String,String> 
          @stack = new Stack <object> 
            
          def test:void
            Condition:
            var i = 0
            i += 1
            Console.WriteLine i
            if i < 10 and -1 < i
              jump Condition
            end
            var result = i
            dict.Clear ()
            stack.Clear ()

            Console.WriteLine "Good bye with result: {0}", result
          end

          def set_position:void x:float y:float z:float
            transform.position = new Vector3 with
              transform.position.x + x
              transform.position.y + y
              transform.position.z + z 
            end
          end
             
          def Update:void
            if Input.GetKey KeyCode.DownArrow
              set_position 0f, -0.1f, 0f
            end
          end
        end

  ... into this: 

        using System;
        using UnityEngine;
        using System.Collections.Generic;

        class Player : MonoBehaviour {
          public String Name = "deulamco" ;
          private Dictionary<String,String> dict = new Dictionary < String ,String > ();
          private Stack<object> stack = new Stack < object > ();

          public void test (  ) {
            Condition:
            var i = 0 ;
            i += (1 );
            Console.WriteLine (i );
            if (i < 10 && -1 < i ){
              goto Condition;
            }
            var result = i ;
            dict.Clear () ;
            dict.Clear () ;
            
            Console.WriteLine ("Good bye with result:{0}", result );
          }

          public void set_position ( float x, float y, float z ) {
            transform.position = new Vector3 ( 
              transform.position.x + x ,
              transform.position.y + y ,
              transform.position.z + z 
            );
          }

          public void Update (  ) {
            if (Input.GetKey (KeyCode.DownArrow)){
              set_position (0f ,-0.1f ,0f );
            }
          }
        }

And then into the Assembly form of DLL or EXE, as the compiler setting at:

    var urb = new UrbCore();
    var source = File.ReadAllText("../../examples/Ruby.rb");
    urb.Compile(source, "demo.dll", isExe: false);

Actually, you can just throw that assembly into Unity Assets folder to use it as a component anyway. 
The intergration can be done later when I finish this in a more tidy way :D

Or you guys can lend me a hand anytime !

####5.Lisp support update
Urb now can also translate Lisp-like language with csharp keyword:

	(require System)
	(require UnityEngine)
	(import System.Collections.Generic)

	(class :public (inherit Player MonoBehaviour)
	    (progn
		(set :public Name "deulamco")
		(set stack (new Stack <object>))
		(set dict (new Dictionary<String,String>)) 

		(defun Start:void
		  (progn
		  	(stack.Clear)
		  	(dict.Clear)))

		(defun :private test:void 
		  (progn 
		  	(label Condition)
			(var i 0)
			(+= i 1)
			(Console.WriteLine i)
			(if (and (< i 10) (< -1 i))
				(jump Condition))
			(var result i)
			(Console.WriteLine "Good bye {0} !" result)))

		(defun :public set_position:void 
			x:float 
			y:float 
			z:float
		  (progn 
		  	(= transform.position 
		    (new Vector3 
		    (+ transform.position.x x)
		    (+ transform.position.y y)
	 	    (+ transform.position.z z)))))
		   
		(defun Update:void
		  (progn
		    (if (Input.GetKey KeyCode.DownArrow)
		    ( set_position 0f -1.0f 0f))))
	    )
	)

 - My aim is to make it has characteristic of Common Lisp syntax & macros, CSharp keywords, and Ruby simplicity.
