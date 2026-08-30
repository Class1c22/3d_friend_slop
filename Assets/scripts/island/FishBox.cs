using UnityEngine;

public class WaterFishZone : MonoBehaviour
{
    public SharkController shark;
    // progressBar тут більше не потрібен - тепер SharkController сам тримає
    // своє (локальне для кожного клієнта) посилання на FishProgressBar
    // і розсилає зміну прогресу через RPC_AddProgress всім гравцям.

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("WaterFishZone: тригер спрацював з " + other.name + ", тег: " + other.tag);

        if (!other.CompareTag("Fish")) return;

        // Риба, яка зараз "у кишені" (щойно спіймана, лежить в інвентарі/руці) -
        // НЕ повинна з'їдатись акулою. Акула має ловити лише рибу, яку гравець
        // свідомо ВИКИНУВ (Pickupable.Drop -> isHeld = false), а не ту, що щойно
        // заспавнилась через PhotonNetwork.Instantiate і ще навіть не в руках.
        // GetComponentInParent, а не GetComponent - на риб'ячому префабі колайдер,
        // що фактично влучає в тригер, часто сидить на ДОЧІРНЬОМУ об'єкті моделі,
        // тоді як сам скрипт Pickupable висить на КОРЕНІ префаба. GetComponent тут
        // повертав би null для такого дочірнього колайдера, і перевірка isHeld
        // мовчки пропускалась би.
        Pickupable pickupable = other.GetComponentInParent<Pickupable>();
        if (pickupable != null && pickupable.isHeld)
        {
            Debug.Log($"[WaterFishZone] {other.name} - риба ще утримується гравцем (isHeld), ігноруємо.");
            return;
        }

        // Якщо знайшли Pickupable (навіть через батьків) - передаємо акулі transform
        // САМЕ КОРЕНЯ риби, а не того дочірнього колайдера, що влучив у тригер.
        // Інакше SharkController.Destroy() знищить лише частину моделі, а порожній
        // батьківський об'єкт лишиться висіти в сцені.
        Transform fishRoot = pickupable != null ? pickupable.transform : other.transform;

        if (shark != null)
            shark.RequestEatFish(fishRoot);
        else
            Debug.LogWarning("[WaterFishZone] Shark не призначено!");
    }
}