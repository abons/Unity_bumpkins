using UnityEngine;

/// <summary>
/// Attach to a ChickenCoop/Chicken object.
/// Produces one egg every config.eggIntervalSeconds automatically — no bumpkin needed.
/// </summary>
public class ChickenCoop : MonoBehaviour
{
    private float _timer;

    void Update()
    {
        if (GameManager.Instance == null) return;

        _timer += Time.deltaTime;
        float interval = GameManager.Instance.config.eggIntervalSeconds;

        if (_timer >= interval)
        {
            _timer -= interval;   // keep remainder, don't reset to 0
            GameManager.Instance.AddEgg();
        }
    }
}
