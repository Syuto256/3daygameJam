using UnityEngine;

public class LuggageSpawner : MonoBehaviour
{

    [SerializeField] private GameObject _prefab;
    [SerializeField] private Transform _generationPosition;
    [SerializeField] private float _generationInterval;
    private float _time;
    
    void Update()
    {
        _time += Time.deltaTime;

        if(_time >= _generationInterval)
        {

            Instantiate(_prefab, _generationPosition.position, Quaternion.identity);
            _time = 0f;

        }


    }
}
