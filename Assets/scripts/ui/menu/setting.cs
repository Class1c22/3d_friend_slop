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
    [SerializeField] private float openDuration = 3f;
    [SerializeField] private float closeDuration = 3f;

    [Header("Кнопка Play")]
    [SerializeField] private string sceneToLoad = "GamePlay";

    // Викликається кнопкою "Налаштування"
    public void OnSettingsButtonClicked()
    {
        SetTrigger("OnSettingClicked");

        // Меню зникає одразу, налаштування з'являються тільки через openDuration секунд
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(ShowAfterDelay(settingsCanvas, openDuration));
    }

    // Викликається кнопкою "Вихід" (на канвасі налаштувань)
    public void OnExitButtonClicked()
    {
        SetTrigger("OnExit");

        // Налаштування зникають одразу, меню з'являється тільки через closeDuration секунд
        if (settingsCanvas != null) settingsCanvas.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(ShowAfterDelay(mainMenuCanvas, closeDuration));
    }

    // Викликається кліком на картинку Play
    public void OnPlayButtonClicked()
    {
        if (mainMenuCanvas != null) mainMenuCanvas.SetActive(false);

        SetTrigger("PlayClicked");

        StopAllCoroutines();
        StartCoroutine(LoadSceneWhenAnimationEnds());
    }

    private void SetTrigger(string triggerName)
    {
        if (sharkAnimator == null)
        {
            Debug.LogWarning($"[SettingsCanvasSwitcher] Shark Animator не призначено - пропускаю тригер '{triggerName}'.");
            return;
        }

        sharkAnimator.SetTrigger(triggerName);
    }

    private IEnumerator ShowAfterDelay(GameObject canvasToShow, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (canvasToShow != null)
        {
            canvasToShow.SetActive(true);
        }
    }

    // Чекає, поки Animator реально дограє анімацію переходу до Play, і лише тоді завантажує сцену
    private IEnumerator LoadSceneWhenAnimationEnds()
    {
        // Чекаємо один кадр, щоб Animator встиг почати перехід після SetTrigger
        yield return null;

        if (sharkAnimator != null)
        {
            // Поки триває сам перехід (blend) між станами - чекаємо
            while (sharkAnimator.IsInTransition(0))
            {
                yield return null;
            }

            // Тепер чекаємо, поки поточний стан (Play) дограє до кінця
            while (sharkAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                yield return null;
            }
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}