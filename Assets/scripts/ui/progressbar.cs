using UnityEngine;

public class FishProgressBar : MonoBehaviour
{
    [Tooltip("Скільки риби треба закинути, щоб бар заповнився повністю")]
    public int fishNeeded = 10;

    [Tooltip("Швидкість руху смужок")]
    public float scrollSpeedX = 1f;
    public float scrollSpeedY = 0f;

    private SpriteRenderer spriteRend;
    private Material matInstance;
    private int currentFish = 0;

    void Start()
    {
        spriteRend = GetComponent<SpriteRenderer>();

        // Створюємо ВЛАСНУ копію матеріалу, щоб не чіпати спільний Sprite-Unlit-Default
        matInstance = new Material(spriteRend.sharedMaterial);
        spriteRend.material = matInstance;
    }

    void Update()
    {
        float offsetX = Time.time * scrollSpeedX;
        float offsetY = Time.time * scrollSpeedY;
        matInstance.mainTextureOffset = new Vector2(offsetX, offsetY);
    }

    public void AddFish(int amount = 1)
    {
        currentFish += amount;
        currentFish = Mathf.Clamp(currentFish, 0, fishNeeded);
        // тут можна оновити fillAmount, якщо додасте окремий Image для заповнення
        if (currentFish >= fishNeeded)
        {
            Debug.Log("Бар заповнено!");
        }
    }

    void OnDestroy()
    {
        if (matInstance != null)
            Destroy(matInstance);
    }
}