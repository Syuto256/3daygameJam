using UnityEngine;

public class LuggageRecycleArea : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Luggage luggage = other.GetComponent<Luggage>();
        GameObject obj = luggage.gameObject;
        obj.SetActive(false);
        
    }

}
