# Bailey
A minimal forth language inpired by lisp/ruby/forth/ML.

####1. Intend
 - a thin-layer of csharp transformation.
 
####2. Core Features
 - lisp list.
 
 - ruby simplicity.
 
 - ML type inference.
 - ML pattern matching.
 
 - forth stack based.
 - forth post-fix order.
 
 - interactive + compiled.
 - .net Framework compatibility.

####3. Mechanism
   
   - Urb simply transform your language into equal csharp syntax rules, yes, I'm doing the code generation way to experiment the best sample code format. If things goes well, then I will replace with CodeDom or IL Emit (if necessary).

The work flow:

        urb language support > tokenizer > code transformation > csharp syntax > csharp compiler

####4. Examples: file concept.b

    == primitives types: Int32, Float, Double, Bool, String, Symbol
    == data types: List, Stack

    (:isDebug false) => var isDebug = false;
    (:numbers 1 2 3) => var listA = List<Int32>(){1, 2, 3};
    (:symbols a b c) => var listB = List<Symbol>(){a, b, c};
    (:message "Hi!") => var message = "Hi!"; 


    == simple function with type specific.
    (:square/int->int dup *) 
    ===> __________________________________
    ===> int square (int a) {return a * a;}


    == bailey aim for freedom so no special order is required.
    (:square | 0 -> 0 | 1 -> 1 | n -> n n * )
    ===> _____________________________________
    ===> int square (int n) {
    ===>    if(n == 0) return 0; 
    ===>    else if(n == 1) return 1; 
    ===>   else return n * n;
    ===> }


    == or pretty in order a bit.
    (:factorial 
        | 0 -> 0
        | 1 -> 1 
        | n -> n n 1 - factorial * )
    ===> _____________________________________
    ===> int factorial (int n) { 
    ===>    if(n == 0) return 0; 
    ===>    else if(n == 1) return 1; 
    ===>    else return n * factorial (n - 1); 
    ===> }


    == forced into a code block help reduce overloads matching complexity.
    (2 square factorial) ===> 24 
    ==> ________________________
    ===> factorial (square (4));

for real working sample, look into \example. 
It's already able to be compiled and use under Unity as component. 
Though, it need more time to be ready for something serious.

Certainly, this is still experiment.
 You will know when it's ready. 
 The world will know :)
 
 * If you have any interests in language design/creation, please join me at: https://gitter.im/language-creator/Lobby
