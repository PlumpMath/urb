﻿require System
require UnityEngine
import System.Collections.Generic

class UrbDemo < MonoBehaviour

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

		Console.WriteLine "Good bye {0}", result
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
