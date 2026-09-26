using UnityEngine;

public class RouteController : MonoBehaviour
{
    [SerializeField] private RouteType _nowRouteType = RouteType.Center;
    public RouteType CurrentRoute => _nowRouteType;
    
    public void ChangeRoute(RouteType newRouteType)
    {
        _nowRouteType = newRouteType;
    }

}
