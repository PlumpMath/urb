(require System)
(require UnityEngine)
(import System.Collections.Generic)
#############################################
#											#
# 'require' will add reference the library, #
# and using it at the same time, while		#
# 'import' just using the subset.			#
#											#
#############################################

##################################################
#												 #
#  :: class ::									 #
#												 #
# 'inherit' is like a function that output merge #
# of class name and all of its heritance types.  #
#												 #
##################################################
(class :public (inherit Player MonoBehaviour)
	(progn
		# string field
		(set :public Name "deulamco")

		# static field
		(setstatic :public protectedStaticA "I'm static variable.")

		# generic object instances
		(set stack (new Stack <object>))
		(set dict (new Dictionary<String,String>)) 

		# defun or defmethod should be better ?
		(defun Start:void
		  (progn
		  	(stack.Clear)
		  	(dict.Clear)
		  	(= protectedStaticA "modified.")))

		# work for override method.
		(override :public ToString:string
			(progn
				(return "ToString is Overrided !")))

		# define static method.
		(defstatic :private static_method:void
			(progn
				(Console.WriteLine "This is a static method.")))

		(defun :private test:void 
		  (progn 
		  	# label is to jump in code.
		  	(label Condition)
			(var i 0)
			(+= i 1)
			(Console.WriteLine i)
			(if (and (< i 10) (< -1 i))
			# jump to the defined label.
				(jump Condition))
			(var result i)
			(Console.WriteLine "Good bye {0} !" result)))

		(defun :public set_position:void 
			x:float 
			y:float 
			z:float
		  (progn 
		    ####################################
		    # 								   #
		    # '=' assignment is different from #
		    # 'set' because set work outside   # 
		    # method, when '=' is inside.	   #
		    #								   #
		    ####################################
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
