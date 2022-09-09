using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Text;

public class AnalyticsEventService : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private string _serverUrl;
    [SerializeField] private float _cooldownBeforeSend = 1f;
    [SerializeField] private bool _dontDestroyOnLoad = false;

    private readonly Queue<AnalyticsEvent> _events = new Queue<AnalyticsEvent>();

    private void Awake()
    {
        if(_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TrackEvent(new AnalyticsEvent("fdfd", "fdfd"));

        StartCoroutine(SendEventsDataAfterCooldown());
    }

    public void TrackEvent(AnalyticsEvent analyticsEvent)
    {
        if (_events == null)
            return;

        _events.Enqueue(analyticsEvent);
    }

    private IEnumerator SendEventsToServer()
    {
        if (_events == null || _events.Count < 0)
            yield break;

        if (string.IsNullOrEmpty(_serverUrl))
        {
            throw new System.Exception("Server url string is empty!");
        }

        string eventsDataJson = EventsDataToJson();

        UnityWebRequest eventsRequest = UnityWebRequest.Post(_serverUrl, eventsDataJson);
        UploadHandler uploadHandlerForEventRequest = new UploadHandlerRaw(Encoding.UTF8.GetBytes(eventsDataJson));

        eventsRequest.uploadHandler = uploadHandlerForEventRequest;
        eventsRequest.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");

        yield return eventsRequest.SendWebRequest();

        Debug.Log(eventsRequest.downloadHandler.text);
    }

    private IEnumerator SendEventsDataAfterCooldown()
    {
        Debug.Log("Start coroutine");
        float time = 0f;

        while(time.CompareTo(_cooldownBeforeSend) != 1)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            time += 0.1f;
        }

        StartCoroutine(SendEventsToServer());

        Debug.Log("End corputine");
        StartCoroutine(SendEventsDataAfterCooldown());
        yield break;
    }

    private string EventsDataToJson()
    {
        return "{ \"events\":" + JsonConvert.SerializeObject(_events) + "}";
    }
}
