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

####4. Examples

	(class (inherit Player MonoBehaviour)
           (:public)
           (begin
                (set :public Name "deulamco")
                
                (def ( test -> _)
                     ( void -> _)
                     (:private)
                     (begin 
                        (label Condition)
                        (var i 0)
                        (+= i 1)
                        (Console.WriteLine i)
                        (if (and (< i 10) (< -1 i) 
                                 (or true false))
                            (jump Condition))
                        (var result i)
                        (Console.WriteLine "Good bye {0} !" result)))
                        
                (def (set_position -> x y z)
                     (void 	       -> float float float) 
                     (:public)
                     (begin 
                        (= transform.position 
                            (new Vector3 
                                (+ transform.position.x x)
                                (+ transform.position.y y)
                                (+ transform.position.z z)))))))

I was experiment with all language samples transformation that can be translated into the same C# source. Just to find a way to express my thought style the most into programming. So in the end, I borrow from them all the characteristic I like the most.

Certainly, this is still experiment.
 You will know when it's ready. 
 The world will know :)
 
 * If you have any interests in language design/creation, please join me at: https://gitter.im/language-creator/Lobby
