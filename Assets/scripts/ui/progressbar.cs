using UnityEngine;
using UnityEngine.UI;

public class ScrollStripesTexture : MonoBehaviour
{
    public float scrollSpeedX = 1f;
    public float scrollSpeedY = 0f;

    private Image img;
    private Material mat;

    void Start()
    {
        img = GetComponent<Image>();
        mat = img.material; // потрібен власний матеріал з підтримкою тайлінгу
    }

    void Update()
    {
        float offsetX = Time.time * scrollSpeedX;
        float offsetY = Time.time * scrollSpeedY;
        mat.mainTextureOffset = new Vector2(offsetX, offsetY);
    }
}