using UnityEngine;

public class Luggage : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5;
    private Vector2 _moveVector;
    [SerializeField] private RouteType _routeType = RouteType.Center;

    private void Start()
    {
        _moveVector = GetRouteVector(_routeType);
    }

    public enum RouteType
    {
        Left,
        Center,
        Right
    }
    
    private void Update()
    {     
        transform.Translate(_moveVector * _moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D()
    {
        _moveVector = GetRouteVector(_routeType);
        _moveVector = _moveVector.normalized;
        
    }
    private Vector2 GetRouteVector(RouteType routeType)
    {
        switch(routeType)
        {
            case RouteType.Left:
                return new Vector2(-1,-1);
            case RouteType.Center:
                return new Vector2(0,-1);
            case RouteType.Right:
                return new Vector2(1,-1);
            default :
                return new Vector2(0,-1);
        }
    }


}
