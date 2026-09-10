using UnityEngine;

public class WaterFishZone : MonoBehaviour
{
    [Tooltip("Якщо не задано - шукається автоматично через FindObjectOfType, бо акула - об'єкт сцени.")]
    public SharkController shark;

    void Awake()
    {
        if (shark == null)
            shark = FindObjectOfType<SharkController>();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("WaterFishZone: тригер спрацював з " + other.name + ", тег: " + other.tag);

        if (!other.CompareTag("Fish")) return;

        Pickupable pickupable = other.GetComponentInParent<Pickupable>();
        if (pickupable != null && pickupable.isHeld)
        {
            Debug.Log($"[WaterFishZone] {other.name} - риба ще утримується гравцем (isHeld), ігноруємо.");
            return;
        }

        Transform fishRoot = pickupable != null ? pickupable.transform : other.transform;

        if (shark != null)
            shark.RequestEatFish(fishRoot);
        else
            Debug.LogWarning("[WaterFishZone] Shark не призначено і не знайдено на сцені!");
    }
}