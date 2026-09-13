using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Кнопка Play")]
    [SerializeField] private string sceneToLoad = "GamePlay";

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

    // Викликається кліком на картинку Play
    public void OnPlayButtonClicked()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);

        sharkAnimator.SetTrigger("PlayClicked");

        StopAllCoroutines();
        StartCoroutine(LoadSceneWhenAnimationEnds());
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

    // Чекає, поки Animator реально дограє анімацію переходу до Play, і лише тоді завантажує сцену
    private IEnumerator LoadSceneWhenAnimationEnds()
    {
        // Чекаємо один кадр, щоб Animator встиг почати перехід після SetTrigger
        yield return null;

        // Поки триває сам перехід (blend) між станами — чекаємо
        while (sharkAnimator.IsInTransition(0))
        {
            yield return null;
        }

        // Тепер чекаємо, поки поточний стан (Play) дограє до кінця
        while (sharkAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
        {
            yield return null;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}