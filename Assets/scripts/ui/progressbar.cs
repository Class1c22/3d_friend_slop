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

    private float fullScaleX;
    private float leftEdgeX; // фіксована позиція лівого краю бару

    void Start()
    {
        spriteRend = GetComponent<SpriteRenderer>();

        fullScaleX = transform.localScale.x;

        // Обчислюємо позицію лівого краю (з урахуванням pivot по центру)
        leftEdgeX = transform.localPosition.x - (fullScaleX / 2f);

        matInstance = new Material(spriteRend.sharedMaterial);
        spriteRend.material = matInstance;

        UpdateVisual();
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

        UpdateVisual();

        Debug.Log("Риба зарахована! Прогрес: " + currentFish + "/" + fishNeeded);

        if (currentFish >= fishNeeded)
        {
            OnBarFull();
        }
    }

    private void UpdateVisual()
    {
        float progress = (float)currentFish / fishNeeded;
        float newScaleX = progress * fullScaleX;

        // Центр = лівий край + половина нового розміру
        // (це тримає лівий край на місці незалежно від знаку scale)
        Vector3 pos = transform.localPosition;
        pos.x = leftEdgeX + (newScaleX / 2f);
        transform.localPosition = pos;

        Vector3 scale = transform.localScale;
        scale.x = newScaleX;
        transform.localScale = scale;
    }

    private void OnBarFull()
    {
        Debug.Log("Бар заповнено! Риби достатньо.");
    }

    void OnDestroy()
    {
        if (matInstance != null)
            Destroy(matInstance);
    }
}