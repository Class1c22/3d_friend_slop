using UnityEngine;

public class WaterFishZone : MonoBehaviour
{
    public SharkController shark;
    public FishProgressBar progressBar;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("WaterFishZone: тригер спрацював з " + other.name + ", тег: " + other.tag);

        if (!other.CompareTag("Fish")) return;

        // Риба, яка зараз "у кишені" (щойно спіймана, лежить в інвентарі/руці) -
        // НЕ повинна з'їдатись акулою. Акула має ловити лише рибу, яку гравець
        // свідомо ВИКИНУВ (Pickupable.Drop -> isHeld = false), а не ту, що щойно
        // заспавнилась через PhotonNetwork.Instantiate і ще навіть не в руках.
        Pickupable pickupable = other.GetComponent<Pickupable>();
        if (pickupable != null && pickupable.isHeld)
        {
            Debug.Log($"[WaterFishZone] {other.name} - риба ще утримується гравцем (isHeld), ігноруємо.");
            return;
        }

        if (shark != null)
            shark.RequestEatFish(other.transform, progressBar);
        else
            Debug.LogWarning("[WaterFishZone] Shark не призначено!");
    }
}