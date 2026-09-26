using UnityEngine;

public class LuggageSpawner : MonoBehaviour
{

    [SerializeField] private GameObject _prefab;
    [SerializeField] private Transform _generationPosition;
    [SerializeField] private float _generationInterval;
    [SerializeField] private RouteController _routeController;
    private float _time;
    
    void Update()
    {
        _time += Time.deltaTime;

        if(_time >= _generationInterval)
        {

            GameObject obj = Instantiate(_prefab, _generationPosition.position, Quaternion.identity);
            Luggage luggage = obj.GetComponent<Luggage>();
            luggage.SetRouteController(_routeController);
            _time = 0f;

        }


    }
}
