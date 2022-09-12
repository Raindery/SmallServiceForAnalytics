public class UIGetReward : UIBase
{
    private string _awardName = "TestAward";

    protected override string GetFormatedDataString()
    {
        return $"award:{_awardName}";
    }

    protected override void OnClickAction()
    {
        if (AnalyticsEventService.Instance != null)
            AnalyticsEventService.Instance.TrackEvent(new AnalyticsEvent("getAward", GetFormatedDataString()));
    }
}
