using UnityEngine;

public sealed class UIRemoveCoins : UIBase
{
    private int _coinsCountRemoved;

    protected override string GetFormatedDataString()
    {
        return $"coins:{_coinsCountRemoved}";
    }

    protected override void OnClickAction()
    {
        _coinsCountRemoved = Random.Range(1, 100);

        if (AnalyticsEventService.Instance != null)
            AnalyticsEventService.Instance.TrackEvent(new AnalyticsEvent("coinsRemoved", GetFormatedDataString()));
    }
}
