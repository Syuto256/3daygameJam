using UnityEngine;

public class RouteJudge : MonoBehaviour
{
    [SerializeField] private CorrectType _judgeRoute;
    
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        
        Luggage luggage = other.GetComponent<Luggage>();
        if(_judgeRoute == luggage.CorrectRoute)
        {
            Debug.Log("正解");
        }
        else
        {
            Debug.Log("不正解");
        }
        
        

    }

}
