require System
require UnityEngine

class Player < MonoBehaviour

  Dictionary(String:String) new 
  @dict pop

  Stack(Object) new 
  @stack pop
	  
  def test:void
    condition:
      1 i +=
      i Console.WriteLine
      -1 i < and 10 i > if 
        :condition jump
      i result =
	  
    goodbye:
      "Good bye" Console.WriteLine
  end
  
  def set_position:void x:float y:float z:float
    transform.position.x x +
    transform.position.y y +
    transform.position.z z + 
      Vector3 new
    transform.position pop
  end
  
  def Update:void
    KeyCode.DownArrow Input.GetKey if
      0f -0.1f 0f set_position
  end
end
 