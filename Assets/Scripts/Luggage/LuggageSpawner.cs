using UnityEngine;
using System.Collections.Generic;
using Scripts.Core.Event;

public class LuggageSpawner : MonoBehaviour
{

    [SerializeField] private GameObject _prefab;
    [SerializeField] private Transform _generationPosition;
    [SerializeField] private float _generationInterval;
    [SerializeField] private RouteController _routeController;
    private List<Luggage> objList  = new List<Luggage>();
    private float _time;
    private bool _isGameOver = false;

    private void OnEnable()
    {
        EventBus.Subscribe<GameOverEvent>(OnGameOver);
    }
    
    private void OnDisable()
    {
        EventBus.Unsubscribe<GameOverEvent>(OnGameOver);
    }

    private void OnGameOver(GameOverEvent evt)
    {
        _isGameOver = true;
    }

    void Update()
    {
        if(_isGameOver)
        {
            return;
        }
        _time += Time.deltaTime;

        if(_time >= _generationInterval)
        {
            Luggage hideObj = null;
            
            foreach(Luggage nowCheckObj in objList)
            {
                if(nowCheckObj.gameObject.activeSelf == false)
                {
                    hideObj = nowCheckObj;
                    break;
                }

                
            }

            if(hideObj != null)
            {
                hideObj.transform.position = _generationPosition.position;
                hideObj.ResetLuggage();
                hideObj.gameObject.SetActive(true);
            }
            else
            {
                GameObject obj = Instantiate(_prefab, _generationPosition.position, Quaternion.identity);
                Luggage luggage = obj.GetComponent<Luggage>();
                objList.Add(luggage);
                luggage.SetRouteController(_routeController);
            }
            _time = 0f;

        }


    }
}
