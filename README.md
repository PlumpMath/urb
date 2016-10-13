# Urb
Is a Unity-compatible language inpired by ruby/forth.

####1. Intend
 - a C# Transformer from the mix of ruby/forth to let us have fun. I will halt this project if we don't.
 
####2. Core Features
 - Ruby syntax & block.
 - Forth Statement order (Experiment).
 - C# source code & assembly output.
 - Unity Editor intergration.
 
####3. Mechanism

        urb code > tokens > C# syntax > .NET C# Compiler > Assembly > Unity

####4. Examples

  - So how does Urb work ? It turn something like this:

        require System
        require UnityEngine
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

            Console.WriteLine "Good bye"
            return result
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
            Console.WriteLine ("Good bye" );
            return (result );
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
