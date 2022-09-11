using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Text;
using System.IO;
using UnityEngine.UI;
using System.Linq;

public class AnalyticsEventService : MonoBehaviour
{
    private const string EVENT_CACHE_FILE_NAME = "analytics-event-cache.json";
    private const string EVENT_CACHE_PLAYER_PREFS_KEY = "cachedAnalyticsEvents";

    [Header("General")]
    [SerializeField] private Text _text;
    [SerializeField] private string _serverUrl;
    [SerializeField] private float _cooldownBeforeSend = 1f;
    [Min(1)]
    [SerializeField] private int _maxCountTrackedEvents = 1000;
    [SerializeField] private bool _dontDestroyOnLoad = false;

    private readonly Queue<AnalyticsEvent> _events = new Queue<AnalyticsEvent>();
    private string _eventCachePath;
    private bool _isWebGLApp = false;
    

    private void Awake()
    {
#if UNITY_ANDROID
        _eventCachePath = Path.Combine(Application.dataPath, EVENT_CACHE_FILE_NAME);
#elif UNITY_WEBGL
        _isWebGLApp = true;
#else
        _eventCachePath = Path.Combine(Application.dataPath, _eventCacheFileName);
#endif

        if (_dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        TrackEvent(new AnalyticsEvent("fdfd", "fdfd"));
        TrackEvent(new AnalyticsEvent("fdfd", "fdfd"));
        TrackEvent(new AnalyticsEvent("fdfd", "fdfd"));
        TrackEvent(new AnalyticsEvent("fdfd", "fdfd"));
        TrackEvent(new AnalyticsEvent("fdfd", "fdfd"));
        TrackEvent(new AnalyticsEvent("fdfd", "fdfd"));

        CacheTrackedEvents();
        TrackCachedEventsData();
        

        //StartCoroutine(SendEventsDataAfterCooldown());
    }

    public void TrackEvent(AnalyticsEvent analyticsEvent)
    {
        if (_events == null)
            return;

        if (_events.Count >= _maxCountTrackedEvents)
        {
            CacheTrackedEvents();
            _events.Clear();
        }

        _events.Enqueue(analyticsEvent);
    }

    private IEnumerator SendTrackedEventsToServer()
    {
        if (_events == null || _events.Count <= 0)
            yield break;

        if (string.IsNullOrEmpty(_serverUrl))
            throw new System.Exception("Server url string is empty!");

        string eventsDataJson = TrackedEventsDataToJson();

        UnityWebRequest eventsRequest = UnityWebRequest.Post(_serverUrl, eventsDataJson);
        UploadHandler uploadHandlerForEventRequest = new UploadHandlerRaw(Encoding.UTF8.GetBytes(eventsDataJson));

        eventsRequest.uploadHandler = uploadHandlerForEventRequest;
        eventsRequest.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");

        yield return eventsRequest.SendWebRequest();


        if(eventsRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log(eventsRequest.downloadHandler.text);
            _events.Clear();
        }
        else
        {
            Debug.Log(eventsRequest.error);
        }
    }

    private IEnumerator SendTrackedEventsDataAfterCooldown()
    {
        Debug.Log("Start coroutine");
        float time = 0f;

        while(time.CompareTo(_cooldownBeforeSend) != 1)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            time += 0.1f;
        }

        StartCoroutine(SendTrackedEventsToServer());

        Debug.Log("End corputine");
        StartCoroutine(SendTrackedEventsDataAfterCooldown());
        yield break;
    }

    private string TrackedEventsDataToJson()
    {
        return "{ \"events\":" + JsonConvert.SerializeObject(_events) + "}";
    }

    private void CacheTrackedEvents()
    {
        if (_events == null || _events.Count <= 0)
            return;

        string cacheTrackedEventsString = JsonConvert.SerializeObject(_events);

        if (_isWebGLApp)
        {
            if (PlayerPrefs.HasKey(EVENT_CACHE_PLAYER_PREFS_KEY))
            {
                string cachedWebGLEvents = PlayerPrefs.GetString(EVENT_CACHE_PLAYER_PREFS_KEY);
                cacheTrackedEventsString = cachedWebGLEvents + cacheTrackedEventsString;
            }

            PlayerPrefs.SetString(EVENT_CACHE_PLAYER_PREFS_KEY, cacheTrackedEventsString + "\n");
            PlayerPrefs.Save();
            return;
        }

        FileInfo eventCacheFile = new FileInfo(_eventCachePath);

        if (!eventCacheFile.Exists)
            File.Create(_eventCachePath).Close();
        
        File.AppendAllText(_eventCachePath, cacheTrackedEventsString + "\n");
    }

    private void TrackCachedEventsData()
    {
        List<AnalyticsEvent> cachedAnalyticsEvents = new List<AnalyticsEvent>();

        if (_isWebGLApp)
        {
            Debug.Log(PlayerPrefs.GetString(EVENT_CACHE_PLAYER_PREFS_KEY));

            string[] cachedEventsDataStrings = PlayerPrefs.GetString(EVENT_CACHE_PLAYER_PREFS_KEY).Trim(' ', '\n').Split('\n');

            for(int i = 0; i < cachedEventsDataStrings.Length; i++)
            {
                cachedAnalyticsEvents.AddRange(JsonConvert.DeserializeObject<ICollection<AnalyticsEvent>>(cachedEventsDataStrings[i]));
            }

            PlayerPrefs.DeleteKey(EVENT_CACHE_PLAYER_PREFS_KEY);
            PlayerPrefs.Save();
        }
        else
        {
            if (!File.Exists(_eventCachePath))
                return;

            var lines = File.ReadLines(_eventCachePath);
            foreach(string dataLine in lines)
            {
                Debug.Log(dataLine);
                cachedAnalyticsEvents.AddRange(JsonConvert.DeserializeObject<ICollection<AnalyticsEvent>>(dataLine));
            }

            File.Delete(_eventCachePath);
        }

        foreach(AnalyticsEvent cachedEvent in cachedAnalyticsEvents)
        {
            Debug.Log(cachedEvent.DataString());
            _events.Enqueue(cachedEvent);
        }
    }
}
