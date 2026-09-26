using UnityEngine;

public class Luggage : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5;
    private Vector2 _moveVector;
    [SerializeField] private RouteType _routeType = RouteType.Center;
    [SerializeField] private RouteController _routeController;
    [SerializeField] private CorrectType _correctType;
    public CorrectType CorrectRoute => _correctType;
    [SerializeField] private LuggageStatus _luggageType;
    public LuggageStatus CurrentLuggageType => _luggageType;

    private void Start()
    {
        _moveVector = GetRouteVector(_routeType);
        _correctType = (CorrectType)Random.Range(0,3);
        _luggageType = (LuggageStatus)Random.Range(0,2);
    }

    private void Update()
    {
        transform.Translate(_moveVector * _moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("BranchingArea"))
        {
            Debug.Log(_moveVector);
            _routeType = _routeController.CurrentRoute;
            Debug.Log(_routeType);
            /*if(_luggageType == LuggageStatus.Defective)
            {
                if(_routeType == RouteType.Up)
                {

                }
            }*/

            _moveVector = GetRouteVector(_routeType);
            _moveVector = _moveVector.normalized;
        }
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
            case RouteType.Up:
                return new Vector2(0,1);
            default:
                return new Vector2(0,-1);
        }
    }

    public void SetRouteController(RouteController routeController)
    {
        _routeController = routeController;
    }

    public void ResetLuggage()
    {
        _routeType = RouteType.Center;
        _moveVector = GetRouteVector(_routeType);
        _correctType = (CorrectType)Random.Range(0,3);
        _luggageType = (LuggageStatus)Random.Range(0,2);
    }
}
