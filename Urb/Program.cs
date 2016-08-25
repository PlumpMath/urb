using System;

namespace Urb
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			var urb = new UrbCore ();
			Console.WriteLine ("* Urb :: A Rubylike post-fix language compiler *");

			// Test Source:
			string test = @" 
			  require System
			  require UnityEngine
			 
			  class Player < MonoBehaviour
			  
			    Dictionary(String:String) new 
				@dict pop
				   
				Stack(Object) new 
				@stack pop
				  
				def test
				  condition:
				    1 i +=
				    i Console.WriteLine
				    10 i > if 
				      :condition jump
				    end
				    i result =
				  
				  goodbye:
				    ""Good bye"" Console.WriteLine
				    result
				end

				def set_position x:float y:float z:float
				  transform.position.x x +
				  transform.position.y y +
				  transform.position.z z +
				  Vector3 new
				  transform.position pop
				end
				   
				def Update
				  KeyCode.DownArrow Input.GetKey if
				    0f -0.1f 0f set_position
				  end
				end
			  end
";
			urb.Parse (test, isDebug:false);
			//Console.ReadLine ();
		}
	}
}
