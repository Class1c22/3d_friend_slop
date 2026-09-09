using Photon.Pun;
using UnityEngine;

// Повісити на КОРІНЬ префабу гравця (mainhero_animated), поруч з PhotonView.
// Один раз, при спавні, вимикає всі локальні системи керування (рух, камеру,
// підбір предметів, дихання, UI інвентаря) на копіях, що належать ІНШИМ гравцям.
//
// Без цього кожна заспавнена копія (включно з чужими) читає локальний Input
// цього клієнта - тобто WASD/миша/E керують ВСІМА аватарами в сцені одночасно,
// а UI кисню показувався б навіть за чужий кисень.
[RequireComponent(typeof(PhotonView))]
public class PlayerRig : MonoBehaviourPun
{
    [Header("Скрипти, що мають працювати ЛИШЕ на своєму (IsMine) аватарі")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private FirstPersonCamera firstPersonCamera;
    [SerializeField] private PlayerPickup playerPickup;
    [SerializeField] private PlayerBreath playerBreath;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private AudioListener audioListener;

    [Header("UI, який мають бачити тільки ми самі (напр. Canvas інвентаря, бар кисню)")]
    [SerializeField] private GameObject[] localOnlyUI;

    void Awake()
    {
        bool mine = photonView.IsMine;

        if (playerController != null) playerController.enabled = mine;
        if (firstPersonCamera != null) firstPersonCamera.enabled = mine;
        if (playerPickup != null) playerPickup.enabled = mine;
        if (playerBreath != null) playerBreath.enabled = mine;
        if (playerCamera != null) playerCamera.gameObject.SetActive(mine);
        if (audioListener != null) audioListener.enabled = mine;

        if (localOnlyUI != null)
        {
            foreach (var ui in localOnlyUI)
                if (ui != null) ui.SetActive(mine);
        }

        // HandAnimatorController НЕ вимикаємо повністю - руки чужих гравців
        // мають рухатись (їх параметри Animator прийдуть по мережі через
        // Photon Animator View). Але свій локальний Input у ньому теж
        // треба заглушити на чужих копіях - це зроблено всередині самого
        // HandAnimatorController.cs через перевірку photonView.IsMine.
    }
}