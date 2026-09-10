using UnityEngine;
using UnityEngine.UI;

// ВАЖЛИВО про мультиплеєр: цей скрипт НЕ потребує PhotonView і жодних RPC.
// Він підписується на HeightmapIsland.OnBite / OnIslandDevoured, а ці події
// й так спрацьовують ОДНАКОВО на кожному клієнті (бо викликаються з
// RPC_BiteAt, який прийшов через RpcTarget.All - див. коментарі в
// HeightmapIsland). Тобто бар автоматично синхронний "безкоштовно".
public class IslandHealthBar : MonoBehaviour
{
    [Header("Джерела даних (якщо не призначити вручну - знайдуться самі на сцені)")]
    public HeightmapIsland island;
    [Tooltip("Потрібен лише щоб дізнатись totalBites - скільки укусів = 100% -> 0% здоров'я.")]
    public SharkBiteController biteController;

    [Header("UI")]
    [Tooltip("Image типу Filled (Horizontal/Radial) - fillAmount буде здоров'ям острова від 1 до 0.")]
    public Image fillImage;
    [Tooltip("Колір при повному здоров'ї.")]
    public Color fullHealthColor = Color.green;
    [Tooltip("Колір при нулі здоров'я.")]
    public Color zeroHealthColor = Color.red;

    [Header("Анімація")]
    [Tooltip("Швидкість, з якою видиме значення бару доганяє реальне (чим більше - тим різкіше).")]
    public float catchUpSpeed = 2f;

    private int bitesReceived = 0;
    private float targetHealth01 = 1f;
    private float displayedHealth01 = 1f;

    void Awake()
    {
        // Автопошук, якщо поля не призначені в інспекторі. Робимо саме в
        // Awake (а не OnEnable), щоб посилання гарантовано були готові
        // ще ДО того, як OnEnable підпишеться на події острова.
        if (island == null)
        {
            island = FindAnyObjectByType<HeightmapIsland>();
            if (island == null)
                Debug.LogWarning("[IslandHealthBar] Не знайдено HeightmapIsland на сцені.");
        }

        if (biteController == null)
        {
            biteController = FindAnyObjectByType<SharkBiteController>();
            if (biteController == null)
                Debug.LogWarning("[IslandHealthBar] Не знайдено SharkBiteController на сцені - totalBites за замовчуванням = 1.");
        }

        if (fillImage == null)
            Debug.LogWarning("[IslandHealthBar] Fill Image не призначено - бар не буде відображатись.");
    }

    void OnEnable()
    {
        if (island == null) return;

        island.OnBite += HandleBite;
        island.OnIslandDevoured += HandleDevoured;

        bitesReceived = 0;
        targetHealth01 = 1f;
        displayedHealth01 = 1f;
        ApplyVisual();
    }

    void OnDisable()
    {
        if (island == null) return;

        island.OnBite -= HandleBite;
        island.OnIslandDevoured -= HandleDevoured;
    }

    // Викликається на КОЖЕН укус (і звичайний, і фінальний) - радіус тут
    // не використовуємо напряму, бо SharkBiteController вже підбирає
    // радіус звичайних укусів так, щоб їх було рівно totalBites штук.
    private void HandleBite(Vector3 worldPos, float radius)
    {
        bitesReceived++;

        int totalBites = (biteController != null) ? biteController.totalBites : 1;
        targetHealth01 = Mathf.Clamp01(1f - (float)bitesReceived / totalBites);
    }

    // Фінальне "доїдання" острова (звичайний останній укус АБО
    // DevourWholeIslandNow при смерті гравця) - у будь-якому разі здоров'я
    // гарантовано падає до нуля, незалежно від bitesReceived.
    private void HandleDevoured(float sinkDuration)
    {
        targetHealth01 = 0f;
    }

    void Update()
    {
        if (fillImage == null) return;

        displayedHealth01 = Mathf.MoveTowards(displayedHealth01, targetHealth01, catchUpSpeed * Time.deltaTime);
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (fillImage == null) return;

        fillImage.fillAmount = displayedHealth01;
        fillImage.color = Color.Lerp(zeroHealthColor, fullHealthColor, displayedHealth01);
    }
}