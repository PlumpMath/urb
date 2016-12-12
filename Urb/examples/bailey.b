;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;
;;                                                 ;;
;;       Bailey :: a high level IL language        ;;
;;      * based on asmrb prototype on ruby. *      ;;
;;                                                 ;;
;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;;

fun factorial
    int [acc n]
    psh [1 n]
    jge cont

    psh acc
    cal puts
    ret

    blo cont
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
    for var token
    in  tokens
    do  :eating
    cal PrintStack
    ret

    blo :eating
    psh token userVars
    cal EatToken
end
