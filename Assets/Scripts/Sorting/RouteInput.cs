using UnityEngine;
using UnityEngine.InputSystem;

public class RouteInput : MonoBehaviour
{
    [SerializeField] private RouteController _routeController;
    [SerializeField] private RouteView _routeView;
    [SerializeField] private RouteSwitchPunch _routeSwitchPunch;

    public void OnLeft(InputAction.CallbackContext left)
    {
        if(left.performed)
        {
            ChangeRoute(RouteType.Left);
        }
    }

    public void OnCenter(InputAction.CallbackContext center)
    {
        if(center.performed)
        {
            ChangeRoute(RouteType.Center);
        }
    }

    public void OnRight(InputAction.CallbackContext right)
    {
        if(right.performed)
        {
            ChangeRoute(RouteType.Right);
        }
    }

    public void OnUp(InputAction.CallbackContext up)
    {
        if(up.performed)
        {
            Debug.Log("OnUp");
            ChangeRoute(RouteType.Up);
        }
    }

    private void ChangeRoute(RouteType routeType)
    {
        _routeController.ChangeRoute(routeType);
        if(_routeView != null) _routeView.UpdateRouteView(routeType);
        if(_routeSwitchPunch != null) _routeSwitchPunch.Play();
    }

}
