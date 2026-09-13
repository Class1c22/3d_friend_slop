using UnityEngine;
using System.Collections;

public class SettingsCanvasSwitcher : MonoBehaviour
{
    [Header("Канваси для перемикання")]
    [SerializeField] private GameObject mainMenuCanvas;
    [SerializeField] private GameObject settingsCanvas;

    [Header("Animator, що грає menusetting")]
    [SerializeField] private Animator sharkAnimator;

    [Header("Тривалість анімації переходу (сек)")]
    [SerializeField] private float openDuration = 1f;
    [SerializeField] private float closeDuration = 1f;

    // Викликається кнопкою "Налаштування"
    public void OnSettingsButtonClicked()
    {
        sharkAnimator.SetTrigger("OnSettingClicked");
        StopAllCoroutines();
        StartCoroutine(SwitchAfterDelay(openDuration, toSettings: true));
    }

    // Викликається кнопкою "Вихід" (на канвасі налаштувань)
    public void OnExitButtonClicked()
    {
        sharkAnimator.SetTrigger("OnExit");
        StopAllCoroutines();
        StartCoroutine(SwitchAfterDelay(closeDuration, toSettings: false));
    }

    private IEnumerator SwitchAfterDelay(float delay, bool toSettings)
    {
        yield return new WaitForSeconds(delay);

        if (toSettings)
        {
            if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);
            if (settingsCanvas != null) settingsCanvas.SetActive(true);
        }
        else
        {
            if (settingsCanvas != null) settingsCanvas.SetActive(false);
            if (mainMenuCanvas != null) mainMenuCanvas.SetActive(true);
        }
    }
}