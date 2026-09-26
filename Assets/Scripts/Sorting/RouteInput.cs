using UnityEngine;
using UnityEngine.InputSystem;

public class RouteInput : MonoBehaviour
{
    [SerializeField] private RouteController _routeController;
    
    public void OnLeft(InputAction.CallbackContext left)
    {
        if(left.performed)
        {
            
            _routeController.ChangeRoute(RouteType.Left);
        }
    }

    public void OnCenter(InputAction.CallbackContext center)
    {
        if(center.performed)
        {
            
            _routeController.ChangeRoute(RouteType.Center);
        }
    }

    public void OnRight(InputAction.CallbackContext right)
    {
        if(right.performed)
        {
            
            _routeController.ChangeRoute(RouteType.Right);
        }
    }

}
