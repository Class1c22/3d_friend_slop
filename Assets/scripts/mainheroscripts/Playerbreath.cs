using Photon.Pun;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // якщо URP; для HDRP - UnityEngine.Rendering.HighDefinition

// Дихання/кисень - суто локальний стан свого гравця (як і рух чи камера).
// Іншим клієнтам не потрібно знати про чужий рівень кисню чи бачити чужий
// UI-бар, тому скрипт вимикається на чужих копіях так само, як
// PlayerController і FirstPersonCamera.
//
// UI кисню зроблено як ДВА Image (Type = Filled, Method = Horizontal),
// що спадають одночасно з обох боків до центру:
//  - oxygenImageLeft:  Fill Origin = Right (ховається зліва)
//  - oxygenImageRight: Fill Origin = Left  (ховається справа)
// Обидва отримують однаковий fillAmount = currentOxygen / maxOxygen.
//
// ВАЖЛИВО: WaterZone більше НЕ вирішує "гравець під водою чи ні" - він лише
// повідомляє через SetInWaterVolume(), що ТІЛО гравця перебуває в об'ємі
// води, і передає висоту поверхні. Тут, у Update(), ми звіряємо позицію
// КАМЕРИ з цією висотою - бар кисню (і витрата кисню) вмикається тільки
// коли камера реально опустилась нижче поверхні, а не коли тіло торкнулось
// тригера.
[RequireComponent(typeof(PhotonView))]
public class PlayerBreath : MonoBehaviourPun
{
    [Header("UI")]
    [Tooltip("Ліва половина бару. Image Type = Filled, Fill Method = Horizontal, Fill Origin = Right")]
    public Image oxygenImageLeft;
    [Tooltip("Права половина бару. Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left")]
    public Image oxygenImageRight;
    [Tooltip("Батьківський об'єкт бару (весь Canvas-елемент, що містить обидві половини), який треба ховати/показувати цілком")]
    public GameObject oxygenBarRoot;

    [Header("Параметри кисню")]
    public float maxOxygen = 100f;
    public float depleteRate = 10f;   // одиниць/сек під водою
    public float refillRate = 25f;    // одиниць/сек на поверхні

    [Header("Vignette (ефект нестачі кисню)")]
    [Tooltip("Global Volume зі сцени, у профілі якого є override Vignette і Film Grain")]
    public Volume postProcessVolume;
    [Tooltip("Intensity віньєтки, коли кисню повно")]
    public float vignetteMinIntensity = 0.2f;
    [Tooltip("Intensity віньєтки, коли кисень на нулі")]
    public float vignetteMaxIntensity = 0.491f;
    private Vignette vignette;
    private bool hasVignette;

    [Header("Film Grain (ефект нестачі кисню)")]
    [Tooltip("Intensity зерна, коли кисню повно")]
    public float filmGrainMinIntensity = 0.158f;
    [Tooltip("Intensity зерна, коли кисень на нулі")]
    public float filmGrainMaxIntensity = 0.6f;
    private FilmGrain filmGrain;
    private bool hasFilmGrain;

    [Header("Посилання")]
    public PlayerController playerController; // щоб повідомляти про underwater-гравітацію
    [Tooltip("Камера гравця, за позицією якої визначається занурення. Якщо не задано - береться з playerController.cameraTransform")]
    public Transform cameraTransform;

    private float currentOxygen;
    private bool isUnderwater;      // камера реально нижче поверхні - витрачається кисень, показаний бар
    private bool isInWaterVolume;   // тіло в тригері води (може бути true, коли камера ще над поверхнею)
    private float waterSurfaceY;

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

        if (cameraTransform == null && playerController != null)
            cameraTransform = playerController.cameraTransform;

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            hasVignette = postProcessVolume.profile.TryGet(out vignette);
            hasFilmGrain = postProcessVolume.profile.TryGet(out filmGrain);
        }

        currentOxygen = maxOxygen;

        UpdateBarVisual();
        UpdateOxygenEffects();

        if (oxygenBarRoot != null)
            oxygenBarRoot.SetActive(false);
    }

    void Update()
    {
        UpdateSubmergedState();

        if (isUnderwater)
        {
            currentOxygen -= depleteRate * Time.deltaTime;
            currentOxygen = Mathf.Max(currentOxygen, 0f);

            UpdateBarVisual();
            UpdateOxygenEffects();

            if (currentOxygen <= 0f)
                Die();
        }
        else if (currentOxygen < maxOxygen)
        {
            currentOxygen += refillRate * Time.deltaTime;
            currentOxygen = Mathf.Min(currentOxygen, maxOxygen);

            UpdateBarVisual();
            UpdateOxygenEffects();

            // Бар ховаємо саме тут, а не лише в ExitWater(): якщо гравець
            // вийшов з води з неповним киснем, момент досягнення максимуму
            // настане пізніше, під час одного з наступних кадрів Update().
            if (currentOxygen >= maxOxygen && oxygenBarRoot != null)
                oxygenBarRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Звіряє позицію камери з висотою поверхні води і викликає
    /// EnterWater()/ExitWater() рівно в момент перетину поверхні.
    /// </summary>
    private void UpdateSubmergedState()
    {
        bool shouldBeUnderwater = isInWaterVolume
            && cameraTransform != null
            && cameraTransform.position.y < waterSurfaceY;

        if (shouldBeUnderwater && !isUnderwater)
            EnterWater();
        else if (!shouldBeUnderwater && isUnderwater)
            ExitWater();
    }

    private void UpdateBarVisual()
    {
        float fill = currentOxygen / maxOxygen;

        if (oxygenImageLeft != null)
            oxygenImageLeft.fillAmount = fill;

        if (oxygenImageRight != null)
            oxygenImageRight.fillAmount = fill;
    }

    /// <summary>
    /// Чим менше кисню - тим сильніше візуальні ефекти на Volume:
    /// Vignette Intensity і Film Grain Intensity ростуть від min до max.
    /// При повному кисні = min, при нулі = max.
    /// </summary>
    private void UpdateOxygenEffects()
    {
        float ratio = currentOxygen / maxOxygen; // 1 = повний кисень, 0 = задихається

        if (hasVignette && vignette != null)
        {
            float vIntensity = Mathf.Lerp(vignetteMaxIntensity, vignetteMinIntensity, ratio);
            vignette.intensity.Override(vIntensity);
        }

        if (hasFilmGrain && filmGrain != null)
        {
            float gIntensity = Mathf.Lerp(filmGrainMaxIntensity, filmGrainMinIntensity, ratio);
            filmGrain.intensity.Override(gIntensity);
        }
    }

    /// <summary>
    /// Викликається WaterZone при вході/виході тіла гравця з об'єму води.
    /// Це ще НЕ означає занурення камери - лише те, що гравець у зоні,
    /// де потенційно можливе занурення. surfaceY має сенс лише коли
    /// inVolume = true.
    /// </summary>
    public void SetInWaterVolume(bool inVolume, float surfaceY)
    {
        isInWaterVolume = inVolume;
        waterSurfaceY = surfaceY;

        // Якщо тіло вийшло із зони води - камера точно не під водою,
        // одразу форсуємо вихід, не чекаючи наступного Update.
        if (!inVolume && isUnderwater)
            ExitWater();
    }

    private void EnterWater()
    {
        isUnderwater = true;

        Debug.Log($"[PlayerBreath] {gameObject.name}: камера занурилась під воду. Кисень: {currentOxygen}/{maxOxygen}");

        if (oxygenBarRoot != null) oxygenBarRoot.SetActive(true);
        if (playerController != null) playerController.SetUnderwater(true);
    }

    private void ExitWater()
    {
        isUnderwater = false;

        Debug.Log($"[PlayerBreath] {gameObject.name}: камера вийшла з-під води.");

        if (playerController != null) playerController.SetUnderwater(false);
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