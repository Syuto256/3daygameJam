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
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] Sprite _leftSprite;
    [SerializeField] Sprite _centerSprite;
    [SerializeField] Sprite _rightSprite;
    [SerializeField] Sprite _defectiveLeftSprite;    
    [SerializeField] Sprite _defectiveCenterSprite;
    [SerializeField] Sprite _defectiveRightSprite;

    private void Start()
    {
        _moveVector = GetRouteVector(_routeType);
        _correctType = (CorrectType)Random.Range(0,3);
        RandomDefective();
        ChangeSprite();
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
        RandomDefective();
        ChangeSprite();
    }

    private void RandomDefective()
    {
        
        if(Random.Range(0,10) == 1)
        {
            _luggageType = LuggageStatus.Defective;
        }
        else
        {
            _luggageType = LuggageStatus.Normal;
        }
    }

    private void ChangeSprite()
    {
        if(_luggageType == LuggageStatus.Normal)
        {
            switch(_correctType)
            {
                case CorrectType.Left:
                _spriteRenderer.sprite = _leftSprite;
                return;

                case CorrectType.Center:
                _spriteRenderer.sprite = _centerSprite;
                return;

                case CorrectType.Right:
                _spriteRenderer.sprite = _rightSprite;
                return;
            }
        }
        else
        {
            switch(_correctType)
            {
                case CorrectType.Left:
                _spriteRenderer.sprite = _defectiveLeftSprite; 
                return;

                case CorrectType.Center:
                _spriteRenderer.sprite = _defectiveCenterSprite; 
                return;

                case CorrectType.Right:
                _spriteRenderer.sprite = _defectiveRightSprite; 
                return;
            }

        }
    }
}
