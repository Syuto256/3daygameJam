using UnityEngine;
using UnityEngine.InputSystem;

public class RouteInput : MonoBehaviour
{
    [SerializeField] private RouteController _routeController;
    [SerializeField] private RouteView _routeView;
    
    public void OnLeft(InputAction.CallbackContext left)
    {
        if(left.performed)
        {
            _routeController.ChangeRoute(RouteType.Left);
            _routeView?.UpdateRouteView(RouteType.Left);
        }
    }

    public void OnCenter(InputAction.CallbackContext center)
    {
        if(center.performed)
        {        
            _routeController.ChangeRoute(RouteType.Center);
            _routeView?.UpdateRouteView(RouteType.Center);
        }
    }

    public void OnRight(InputAction.CallbackContext right)
    {
        if(right.performed)
        {
            _routeController.ChangeRoute(RouteType.Right);
            _routeView?.UpdateRouteView(RouteType.Right);
        }
    }

}
