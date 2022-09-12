using UnityEngine;
using UnityEngine.SceneManagement;

public class UIStartLevel : UIBase
{
    [SerializeField] private int _levelSceneIndex;
    private int _levelNumber;

    protected override string GetFormatedDataString()
    {
        return $"level: {_levelNumber}";
    }

    protected override void OnClickAction()
    {
        _levelNumber = Random.Range(1, 20);

        if (AnalyticsEventService.Instance != null)
            AnalyticsEventService.Instance.TrackEvent(new AnalyticsEvent("startLevel", GetFormatedDataString()));

        SceneManager.LoadSceneAsync(_levelSceneIndex, LoadSceneMode.Single);
    }
}
