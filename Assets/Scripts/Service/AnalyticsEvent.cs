using Newtonsoft.Json;

[System.Serializable]
public struct AnalyticsEvent
{
    [JsonProperty("type")]
    private readonly string _type;

    [JsonProperty("data")]
    private readonly string _data;

    public AnalyticsEvent(string type, string data)
    {
        _type = type;
        _data = data;
    }

    public string GetFormatDataString()
    {
        return $"Type: {_type}\nData:[{_data}]";
    }
}
