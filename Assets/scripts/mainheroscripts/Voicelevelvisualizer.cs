using UnityEngine;
using Photon.Voice.Unity;
using Photon.Voice.PUN;

/// <summary>
/// Перемикає текстуру об'єкта залежно від рівня гучності голосу гравця
/// (тиша / нормальна мова / гучна мова — як у PUBG).
/// Вішається на той самий об'єкт, де є PhotonVoiceView (наприклад mainhero_animated).
/// </summary>
[RequireComponent(typeof(PhotonVoiceView))]
public class VoiceLevelVisualizer : MonoBehaviour
{
    [Header("Куди виводити текстуру")]
    [Tooltip("Renderer, на якому буде мінятись mainTexture")]
    public Renderer targetRenderer;

    [Header("Текстури")]
    public Texture silentTexture;   // гравець мовчить
    public Texture normalTexture;   // говорить нормально
    public Texture loudTexture;     // говорить голосно

    [Header("Пороги гучності (RMS, орієнтовно 0..1)")]
    [Tooltip("Нижче цього значення — вважається тишею")]
    public float silentThreshold = 0.02f;
    [Tooltip("Вище цього значення — вважається гучною мовою")]
    public float loudThreshold = 0.15f;

    [Header("Згладжування")]
    [Tooltip("Швидкість реакції індикатора (більше = різкіше)")]
    public float smoothing = 8f;

    private AudioSource _audioSource;
    private readonly float[] _sampleBuffer = new float[256];
    private float _smoothedLevel;
    private Texture _currentTexture;

    private void Start()
    {
        // Speaker відтворює вхідний голос через власний AudioSource
        var speaker = GetComponentInChildren<Speaker>();
        if (speaker != null)
            _audioSource = speaker.GetComponent<AudioSource>();

        if (_audioSource == null)
            Debug.LogWarning("[VoiceLevelVisualizer] Не знайдено AudioSource у Speaker — перевірте, чи Speaker призначено.");

        SetTexture(silentTexture);
    }

    private void Update()
    {
        if (_audioSource == null || !_audioSource.isPlaying)
        {
            _smoothedLevel = Mathf.Lerp(_smoothedLevel, 0f, Time.deltaTime * smoothing);
        }
        else
        {
            _audioSource.GetOutputData(_sampleBuffer, 0);

            float sum = 0f;
            for (int i = 0; i < _sampleBuffer.Length; i++)
                sum += _sampleBuffer[i] * _sampleBuffer[i];

            float rms = Mathf.Sqrt(sum / _sampleBuffer.Length);
            _smoothedLevel = Mathf.Lerp(_smoothedLevel, rms, Time.deltaTime * smoothing);
        }

        if (_smoothedLevel < silentThreshold)
            SetTexture(silentTexture);
        else if (_smoothedLevel < loudThreshold)
            SetTexture(normalTexture);
        else
            SetTexture(loudTexture);
    }

    private void SetTexture(Texture tex)
    {
        if (tex == _currentTexture || targetRenderer == null) return;
        _currentTexture = tex;
        targetRenderer.material.mainTexture = tex;
    }
}