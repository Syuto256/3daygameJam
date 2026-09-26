using UnityEngine;

public class Luggage : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5;
    private Vector2 _moveVector;
    [SerializeField] private RouteType _routeType = RouteType.Center;
    [SerializeField] private RouteController _routeController;
    [SerializeField] private CorrectType _correctType;
    public CorrectType CorrectRoute => _correctType;
    

    private void Start()
    {
        _moveVector = GetRouteVector(_routeType);
        _correctType = (CorrectType)Random.Range(0,3);
    }

    private void Update()
    {     
        transform.Translate(_moveVector * _moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D()
    {
        _routeType = _routeController.CurrentRoute;
        _moveVector = GetRouteVector(_routeType);
        _moveVector = _moveVector.normalized;
        
    }
    private Vector2 GetRouteVector(RouteType routeType)
    {
        switch(routeType)
        {
            case RouteType.Left:
                return new Vector2(-3,-1);
            case RouteType.Center:
                return new Vector2(0,-1);
            case RouteType.Right:
                return new Vector2(3,-1);
            default :
                return new Vector2(0,-1);
        }
    }

    public void SetRouteController(RouteController routeController)
    {
        _routeController = routeController;
    }


}
