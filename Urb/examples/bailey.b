;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;;                                                ;;;
;;;       Bailey :: a high level IL language       ;;;
;;;      * based on asmrb prototype on ruby. *     ;;;
;;;                                                ;;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

load System
using System.Collection.Generic

extends Object

fun factorial :static :public
    int [acc n]
    psh [1 n]
    jge :cont

    ;;; mean to return acc.
    psh acc     
    ret

    ;;; :cont as a label.
    blo :cont
    psh [acc n]
    mul
    psh [1 n]
    sub
    rec
end

fun eval
    arg List body
    arg Dictionary<string,object> env
    for var o
    in  body
    do  :eating
    ret
 
    blo :eating
    psh [o as Token] env
    cal EatToken
end

fun repl
    arg string source
    psh source
    cal Reader
    var tokens
    for var token
    in  tokens
    do  :eating
    cal PrintStack
    ret

    blo :eating
    psh token userVars
    cal EatToken
end
