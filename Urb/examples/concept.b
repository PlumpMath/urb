=======================================================
==                                                   ==
==    :: bailey - a stack-based lisp language ::     ==
==                                                   ==
=======================================================

== : is attractor operator, which attract all tokens in a block into it like a definition.
== [] is un-eval brace annotation.
== << is assignment operator.
== functional pattern matching feature by default.
== |  guard for case matching.
== -> annotation for next act if case matched.


== primitives types: Int32, Float, Double, Bool, String, Symbol
== data types: List, Stack

(:isDebug false) => var isDebug = false;
(:numbers 1 2 3) => var listA = List<Int32>(){1, 2, 3};
(:symbols a b c) => var listB = List<Symbol>(){a, b, c};
(:message "Hi!") => var message = "Hi!"; 


== simple function with type specific.
(:square/int->int dup *) 
===> int square (int a) {return a * a;}


== bailey aim for freedom so no special order is required.
(:square | 0 -> 0 | 1 -> 1 | n -> n n * )
===> int square (int n) {if(n == 0) return 0; else if(n == 1) return 1; else return n * n;}


== or pretty in order a bit.
(:factorial 
    | 0 -> 0
    | 1 -> 1 
    | n -> n n 1 - factorial * )
===> int factorial (int n) { if(n == 0) return 0; else if(n == 1) return 1; else return n * factorial (n - 1); }


== forced into a code block help reduce overloads matching complexity.
(2 square factorial) ===> 24 
===> factorial (square (4));


== mapping function as 1st class.
(System System.IO [ using ] map)
===> using System;
===> using System.IO;


== unknown typing will be delayed.
(:sum [ + ] reduce )


== generic list processing.
(:list-split
	| []         ->                          "empty" print 
	| head::tail -> head tail "head {0} :: tail {1}" print )
===> void list_split(List<object> arg0){ 
===>     if(arg0.Count == 0) print "empty"; 
===>     else { 
===>         var head = arg0[0];
===>         arg0.Remove(head);
===>         var tail = arg0;
===>         print ("head {0} :: tail {1}", head, tail);
===>     }
===> }


== strong typed depend on type inference.
== different types in pattern matching will be divided into overloads.
(:print 
    | object[]/arg String/str -> arg str Console.WriteLine 
    | String/str              ->     str Console.WriteLine )
===> void print (String str, object[] arg) {
===>     Console.WriteLine (str, arg);
===> }
===> void print (String str) {
===>     Console.WriteLine (str);
===> }


== void return because no function return.
== label help shorten lengthy variables.
(:set-position 
    | 0f 0f 0f -> ignore
    | x  y  z  -> [transform.position] p label 
                  p.x x + p.y y + p.z z + Vector3 new p << )
===> void set_position (float x, float y, float z){
===>     if(x == y == z == 0) return;
===>     else {
===>         transform.position = new Vector3 (
===>             transform.position.x + x,
===>             transform.position.y + y,
===>             transform.position.z + z);
===> }
