using UnityEngine;

public class FishDropZone : MonoBehaviour
{
    public FishProgressBar progressBar;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Fish"))
        {
            progressBar.AddFish(1);
            Destroy(other.gameObject); // прибрати рибу після зарахува
        }
    }
}