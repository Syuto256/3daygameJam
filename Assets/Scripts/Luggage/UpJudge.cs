using UnityEngine;

public class UpJudge : MonoBehaviour
{
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Luggage luggage = other.GetComponent<Luggage>();
        if(luggage.CurrentLuggageType == LuggageStatus.Defective)
        {
            Debug.Log("正解");
        }
        else
        {
            Debug.Log("失敗");
        }
    }

}
