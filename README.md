# UFO
Stay for uForth or micro-forth. 
A minimal Unity-compatible language inpired by lisp/ruby/forth.

####1. Intend
 - a thin-layer of code transformation between csharp, forth and common lisp, with mix of ruby warmmy code style. 
 
####2. Core Features
 - lisp macros.
 - ruby clean syntax style.
 - forth stack and post-fix order in statement.
 - remain csharp familiar keywords.
 - maximum .net Framework compatibility.
 - Unity editor intergration.

####3. Mechanism
   
   - Urb simply transform your language into equal csharp syntax rules, yes, I'm doing the code generation way.

The work flow:

        urb language support > tokenizer > code transformation > csharp syntax > csharp compiler > unity

####4. Examples

	###############################################
	# :: ufo - a stack-based lisp language ::
	#
	# special is like dig a hole to wait for arguments
	# then execute when it has enough.
	# special form can goes for special hole
	# when statement is evaluated post-fix order.
	#
	###############################################
	(def (x:int -> square:int) ((x x *)))

	(12 square) # get eval anyway but don't store.

	#
	# @ - un-eval mode 
	# ! - eval mode
	#
	(12 13 @ square ! map) # goes on evaluation stack for the result.

	(1 2 3 @ + ! reduce) # => 6 on stack.

	(System System.IO @ using ! map)

	(namespace Urb)

	(MainClass :partial :public :static class)

	("deulamco" Name :public set)

	("Static variable." StaticA :public :static set)

	(def (_ -> Main:void) :static :public
		(((ULisp new) uLisp var)))

	(def (_ -> ToString:string) :public :override
		(" ToString is overrided."))

	(def (_ -> static_method:void) :static :private
		((" This's static method." Console.WriteLine)))

	(def (_ -> test:void) :private
		((Condition label)
		 (0 i var)
		 (1 i +=)
		 (i Console.WriteLine)
		 (((-1 i <) (10 i <)
		  ((true false) or) and)
		  (Condition jump) if)
		 (i result var)
		 (result Console.WriteLine)))

	(def (x:float y:float z:float -> set_position:void) :public
		(((
		  (transform.position.z z +)
		  (transform.position.y y +)
		  (transform.position.x x +)
		  Vector3 new) transform.position =)))

	(def (_ -> Update:void)
		(((KeyCode.DownArrow Input.GetKey)
		  (0f -1.0f 0f set_position) if)))

	(endclass)

I was experiment with all language samples transformation that can be translated into the same C# source. Just to find a way to express my thought style the most into programming. So in the end, I borrow from them all the characteristic I like the most.

Certainly, this is still experiment.
 You will know when it's ready. 
 The world will know :)
