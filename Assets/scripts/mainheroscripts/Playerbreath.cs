using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;

// Дихання/кисень - суто локальний стан свого гравця (як і рух чи камера).
// Іншим клієнтам не потрібно знати про чужий рівень кисню чи бачити чужий
// UI-бар, тому скрипт вимикається на чужих копіях так само, як
// PlayerController і FirstPersonCamera.
//
// UI кисню зроблено як звичайний Image (Type = Filled) замість Slider -
// картинка просто заливається/зменшується через fillAmount (0..1), без
// стандартних елементів Slider (Fill Area / Handle і т.д.).
[RequireComponent(typeof(PhotonView))]
public class PlayerBreath : MonoBehaviourPun
{
    [Header("UI")]
    [Tooltip("Image з Image Type = Filled (Fill Method = Radial 360 або Horizontal) - показує рівень кисню через fillAmount")]
    public Image oxygenImage;
    [Tooltip("Батьківський об'єкт бару (весь Canvas-елемент), який треба ховати/показувати цілком - можна лишити тим самим об'єктом, що й oxygenImage, або окремим контейнером")]
    public GameObject oxygenBarRoot;

    [Header("Параметри кисню")]
    public float maxOxygen = 100f;
    public float depleteRate = 10f;   // одиниць/сек під водою
    public float refillRate = 25f;    // одиниць/сек на поверхні

    [Header("Посилання")]
    public PlayerController playerController; // щоб повідомляти про underwater-гравітацію

    private float currentOxygen;
    private bool isUnderwater;

    void Start()
    {
        // Як і в інших локальних скриптах - чужим копіям ця логіка не потрібна.
        if (!photonView.IsMine)
        {
            enabled = false;
            if (oxygenBarRoot != null) oxygenBarRoot.SetActive(false);
            return;
        }

        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        currentOxygen = maxOxygen;

        if (oxygenImage != null)
            oxygenImage.fillAmount = 1f;

        if (oxygenBarRoot != null)
            oxygenBarRoot.SetActive(false);
    }

    void Update()
    {
        if (isUnderwater)
        {
            currentOxygen -= depleteRate * Time.deltaTime;
            currentOxygen = Mathf.Max(currentOxygen, 0f);

            UpdateBarVisual();

            if (currentOxygen <= 0f)
                Die();
        }
        else if (currentOxygen < maxOxygen)
        {
            currentOxygen += refillRate * Time.deltaTime;
            currentOxygen = Mathf.Min(currentOxygen, maxOxygen);

            UpdateBarVisual();
        }
    }

    private void UpdateBarVisual()
    {
        if (oxygenImage != null)
            oxygenImage.fillAmount = currentOxygen / maxOxygen;
    }

    public void EnterWater()
    {
        isUnderwater = true;

        if (oxygenBarRoot != null) oxygenBarRoot.SetActive(true);
        if (playerController != null) playerController.SetUnderwater(true);
    }

    public void ExitWater()
    {
        isUnderwater = false;

        if (playerController != null) playerController.SetUnderwater(false);

        if (oxygenBarRoot != null && currentOxygen >= maxOxygen)
            oxygenBarRoot.SetActive(false);
    }

    private void Die()
    {
        Debug.Log("[PlayerBreath] Гравець задихнувся під водою.");

        // TODO: під'єднати до вашої системи смерті/респавну.
        // Якщо смерть має бути видима іншим гравцям (наприклад анімація),
        // її варто розсилати через RPC так само, як HeightmapIsland розсилає
        // укуси - тобто photonView.RPC(nameof(RPC_Die), RpcTarget.All);
        // Заглушку лишаю тут, бо у вас поки немає окремого PlayerHealth.
    }
}