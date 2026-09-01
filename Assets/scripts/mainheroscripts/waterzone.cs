using UnityEngine;

// Повісити на об'єкт з Box/Mesh Collider (Is Trigger = true), що позначає
// об'єм води. Сам WaterZone більше НЕ вирішує, коли гравець "під водою" -
// він лише повідомляє PlayerBreath, що тіло гравця перебуває в об'ємі води,
// і передає висоту поверхні (верх колайдера). Остаточне рішення "камера під
// водою чи ні" приймає сам PlayerBreath, звіряючи Y камери з цією висотою -
// так бар кисню з'являється/зникає саме коли КАМЕРА перетинає поверхню,
// а не коли тіло торкнулось тригера.
public class WaterZone : MonoBehaviour
{
    private Collider waterCollider;

    void Awake()
    {
        waterCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[WaterZone] OnTriggerEnter: {other.name}, tag = {other.tag}");

        if (!other.CompareTag("Player"))
        {
            Debug.Log($"[WaterZone] Пропущено - тег не 'Player' (реальний тег: {other.tag})");
            return;
        }

        var photonView = other.GetComponent<Photon.Pun.PhotonView>();
        if (photonView == null)
        {
            Debug.Log("[WaterZone] Пропущено - немає PhotonView на об'єкті");
            return;
        }

        if (!photonView.IsMine)
        {
            Debug.Log("[WaterZone] Пропущено - це чужий гравець (IsMine = false)");
            return;
        }

        var breath = other.GetComponent<PlayerBreath>();
        if (breath == null)
        {
            Debug.Log("[WaterZone] Пропущено - немає компонента PlayerBreath");
            return;
        }

        float surfaceY = waterCollider != null ? waterCollider.bounds.max.y : transform.position.y;

        Debug.Log($"[WaterZone] {other.name} у зоні води. Поверхня на Y = {surfaceY}");
        breath.SetInWaterVolume(true, surfaceY);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"[WaterZone] OnTriggerExit: {other.name}, tag = {other.tag}");

        if (!other.CompareTag("Player")) return;

        var photonView = other.GetComponent<Photon.Pun.PhotonView>();
        if (photonView == null || !photonView.IsMine) return;

        var breath = other.GetComponent<PlayerBreath>();
        if (breath != null)
        {
            Debug.Log($"[WaterZone] {other.name} покинув зону води");
            breath.SetInWaterVolume(false, 0f);
        }
    }
}