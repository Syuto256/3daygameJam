using UnityEngine;

public class Luggage : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5;
    private Vector2 _moveVector = new Vector2(0,-1);

    /*private void Start()
    {
        _moveVector = Center;
    }*/

    /*public enum RouteType
    {
        Left,
        Center,
        Right
    }*/
    
    private void Update()
    {     
        transform.Translate(_moveVector * _moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D()
    {
        _moveVector = new Vector2(-1,-1);
        _moveVector = _moveVector.normalized;
        
    }
    /*private RouteType GetRouteVector()
    {
        switch(RouteType)
        case 
    }*/


}
