using UnityEngine;

public class FinishLine : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager manager = FindAnyObjectByType<LevelManager>();
            if (manager != null)
            {
                manager.CompleteLevel();
            }
        }
    }
}