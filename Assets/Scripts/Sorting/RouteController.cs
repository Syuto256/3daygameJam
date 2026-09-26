using UnityEngine;

public class RouteController : MonoBehaviour
{
    [SerializeField] private RouteType _nowRouteType = RouteType.Center;
    public RouteType CurrentRoute => _nowRouteType;
    
    public void ChangeRoute(RouteType newRouteType)
    {
        Debug.Log("引数newRouteTypeは" + newRouteType);
        _nowRouteType = newRouteType;
        Debug.Log("代入後の値は" + _nowRouteType);
    }

}
