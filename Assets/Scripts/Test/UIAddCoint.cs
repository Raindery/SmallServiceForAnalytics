using UnityEngine;

public sealed class UIAddCoint : UIBase
{ 
    private int _addedCointCount;
    protected override string GetFormatedDataString()
    {
        return $"coins:{_addedCointCount}";
    }

    protected override void OnClickAction()
    {
        _addedCointCount = Random.Range(10, 200);

        if (AnalyticsEventService.Instance != null)
            AnalyticsEventService.Instance.TrackEvent(new AnalyticsEvent("addCoins", GetFormatedDataString()));
    }
}
