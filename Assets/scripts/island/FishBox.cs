using UnityEngine;

public class WaterFishZone : MonoBehaviour
{
    public SharkController shark;
    public FishProgressBar progressBar;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("WaterFishZone: тригер спрацював з " + other.name + ", тег: " + other.tag);

        if (other.CompareTag("Fish"))
        {
            if (shark != null)
                shark.RequestEatFish(other.transform, progressBar);
            else
                Debug.LogWarning("[WaterFishZone] Shark не призначено!");
        }
    }
}