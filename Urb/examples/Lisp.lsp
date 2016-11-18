(require System)
(require UnityEngine)
(import System.Collections.Generic)

(class :public (inherit Player MonoBehaviour)
	(progn
		(set :public Name "deulamco")
		(setstatic protectedStaticA "I'm static variable.")

		(set stack (new Stack <object>))
		(set dict (new Dictionary<String,String>)) 

		(defun Start:void
		  (progn
		  	(stack.Clear)
		  	(dict.Clear)))

		(override :public ToString:string
			(progn
				(return "ToString is Overrided !")))

		(defstatic :private static_method:void
			(progn
				(Console.WriteLine "This is a static method.")))

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
