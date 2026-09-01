using UnityEngine;

// Повісити на об'єкт з Box/Mesh Collider (Is Trigger = true), що позначає
// об'єм води. Реагує лише на СВОГО гравця (photonView.IsMine) - чужі копії
// в сцені рухаються не за фізикою локального клієнта, тому їм не потрібно
// міняти власну гравітацію чи кисень з чужого боку.
public class WaterZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var photonView = other.GetComponent<Photon.Pun.PhotonView>();
        if (photonView == null || !photonView.IsMine) return;

        var breath = other.GetComponent<PlayerBreath>();
        if (breath != null) breath.EnterWater();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var photonView = other.GetComponent<Photon.Pun.PhotonView>();
        if (photonView == null || !photonView.IsMine) return;

        var breath = other.GetComponent<PlayerBreath>();
        if (breath != null) breath.ExitWater();
    }
}