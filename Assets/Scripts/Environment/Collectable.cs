using UnityEngine;

public class Collectable : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            LevelManager manager = FindAnyObjectByType<LevelManager>();
            if (manager != null)
            {
                manager.IncrementCollectibleCount();
            }

            // Optional: Spawn particles or play a sound effect here
            Destroy(gameObject);
        }
    }

}
