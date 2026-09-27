using UnityEngine;

/// <summary>
/// バルブなどを一定の速さで回し続ける
/// </summary>
public class SlowRotator : MonoBehaviour
{
    [Header("回転の速さ（度/秒）。マイナスで時計回り")]
    [SerializeField] private float _degreesPerSecond = -30f;

    private void Update()
    {
        transform.Rotate(0f, 0f, _degreesPerSecond * Time.deltaTime);
    }
}
