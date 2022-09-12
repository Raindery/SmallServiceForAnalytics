using UnityEngine;
using UnityEngine.SceneManagement;

public class UIExitLevel : UIBase
{
    [SerializeField] private int _startSceneIndex = 0;
    private int _exitedLevelNumber;

    protected override string GetFormatedDataString()
    {
        return $"exitedLevel:{_exitedLevelNumber}";
    }

    protected override void OnClickAction()
    {
        _exitedLevelNumber = Random.Range(1, 10);

        if (AnalyticsEventService.Instance != null)
            AnalyticsEventService.Instance.TrackEvent(new AnalyticsEvent("exitScene", GetFormatedDataString()));

        SceneManager.LoadSceneAsync(_startSceneIndex);
    }
}
