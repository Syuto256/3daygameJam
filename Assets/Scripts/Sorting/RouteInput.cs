using UnityEngine;
using UnityEngine.InputSystem;

public class RouteInput : MonoBehaviour
{
    [SerializeField] private RouteController _routeController;
    
    public void OnLeft(InputAction.CallbackContext left)
    {
        Debug.Log("Leftが呼ばれた。");
        if(left.performed)
        {
            
            _routeController.ChangeRoute(RouteType.Left);
        }
    }

    public void OnCenter(InputAction.CallbackContext center)
    {
        Debug.Log("Centerが呼ばれた。");
        if(center.performed)
        {
            
            _routeController.ChangeRoute(RouteType.Center);
        }
    }

    public void OnRight(InputAction.CallbackContext right)
    {
        Debug.Log("Rightが呼ばれた。");
        if(right.performed)
        {
            
            _routeController.ChangeRoute(RouteType.Right);
        }
    }

}
