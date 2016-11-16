# Urb
A Unity-compatible language inpired by ruby/forth.

####1. Intend
 - a C# Transformer from the mix of ruby/forth to let us have fun. I will halt this project if we don't.
 
####2. Core Features
 - Ruby block & syntax.
 - Forth Statement order (Experiment).
 - C# source code & assembly output.
 - Unity Editor intergration.
 - Code Intellisense MonoDevelop plugin. (If I have enough power)

####3. Mechanism

        urb code > tokens > C# syntax > .NET C# Compiler > Assembly > Unity

####4. Examples

  - So how does Urb work ? It turn something like this:

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
