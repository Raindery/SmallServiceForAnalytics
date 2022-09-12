using System;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public sealed class AnalyticsEventService : MonoBehaviour
{
    private const string EVENT_CACHE_FILE_NAME = "analytics-event-cache.json";
    private const string EVENT_CACHE_PLAYER_PREFS_KEY = "cachedAnalyticsEvents";

    [Header("General")]
    [SerializeField] private string _serverUrl;
    [SerializeField] private float _cooldownBeforeSend = 1f;
    [Min(50)]
    [SerializeField] private int _maxCountTrackedEvents = 1000;
    [SerializeField] private bool _dontDestroyOnLoad = false;

    private readonly Queue<AnalyticsEvent> _events = new Queue<AnalyticsEvent>();
    private string _eventCachePath;
    private bool _isWebGLApp = false;

    public static AnalyticsEventService Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (string.IsNullOrEmpty(_serverUrl))
            throw new Exception("Server url string is empty!");

        #if UNITY_ANDROID
        _eventCachePath = Path.Combine(Application.dataPath, EVENT_CACHE_FILE_NAME);
        #elif UNITY_WEBGL
        _isWebGLApp = true;
        #else
        _eventCachePath = Path.Combine(Application.dataPath, EVENT_CACHE_FILE_NAME);
        #endif

        if (_dontDestroyOnLoad)
        {
            if (FindObjectsOfType<AnalyticsEventService>().Length > 1)
                Destroy(gameObject);
              
            DontDestroyOnLoad(gameObject);
        }
    }

    private void Start()
    {
        StartCoroutine(SendTrackedEventsDataAfterCooldown());
    }

    private void LateUpdate()
    {
        if (Time.frameCount % 1200 == 0 || Time.frameCount == 1)
        {
            StartCoroutine(AnalyticsServerAccess(isAccess =>
            {
                if (isAccess)
                {
                    List<AnalyticsEvent> cachedAnalyticsEvent = GetCachedEventsData();

                    if (cachedAnalyticsEvent != null && cachedAnalyticsEvent.Count > 0)
                    {
                        StartCoroutine(SendTrackedEventsToServer(cachedAnalyticsEvent.ToArray(), result =>
                        {
                            if (result == UnityWebRequest.Result.Success)
                            {
                                Debug.Log("Cached data sent!");
                                ClearCachedEventsData();
                            }
                            else
                            {
                                Debug.LogError(result.ToString());
                            }
                        }));
                    }
                }
                else
                {
                    Debug.Log("Analytic server is not access!");
                }
            }));
        }
    }

    private void OnApplicationQuit()
    {
        CacheTrackedEvents();
        _events.Clear();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            CacheTrackedEvents();
            _events.Clear();
        }
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

    private IEnumerator SendTrackedEventsToServer(AnalyticsEvent[] events, Action<UnityWebRequest.Result> result = null)
    {
        string eventsDataJson = TrackedEventsDataToJson();
        UnityWebRequest eventsRequest = UnityWebRequest.Post(_serverUrl, eventsDataJson);
        UploadHandler uploadHandlerForEventRequest = new UploadHandlerRaw(Encoding.UTF8.GetBytes(eventsDataJson));
        eventsRequest.uploadHandler = uploadHandlerForEventRequest;
        eventsRequest.SetRequestHeader("Content-Type", "application/json; charset=UTF-8");

        yield return eventsRequest.SendWebRequest();

        result(eventsRequest.result);
        yield break;
    }

    private IEnumerator SendTrackedEventsDataAfterCooldown()
    {
        Debug.Log("Start SendTrackedEventsDataAfterCooldown");
        float time = 0f;

        while(time.CompareTo(_cooldownBeforeSend) != 1)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            time += 0.1f;
        }

        StartCoroutine(SendTrackedEventsToServer(_events.ToArray() ,result =>
        {
            if(result == UnityWebRequest.Result.Success)
                _events.Clear();
            else
                Debug.LogError(result.ToString());
        }));

        Debug.Log("End SendTrackedEventsDataAfterCooldown");
        StartCoroutine(SendTrackedEventsDataAfterCooldown());

        yield break;
    }

    private IEnumerator AnalyticsServerAccess(Action<bool> isAccessCallback)
    {
        UnityWebRequest connectionRequest = UnityWebRequest.Get(_serverUrl);

        yield return connectionRequest.SendWebRequest();

        if (connectionRequest.result == UnityWebRequest.Result.Success)
            isAccessCallback(true);
        else
            isAccessCallback(false);

        yield break;
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
            Debug.Log("Cache tracked events successful in WebGL!");
            return;
        }

        FileInfo eventCacheFile = new FileInfo(_eventCachePath);

        if (!eventCacheFile.Exists)
            File.Create(_eventCachePath).Close();
        
        File.AppendAllText(_eventCachePath, cacheTrackedEventsString + "\n");
        Debug.Log("Cache tracked events successful!");
    }

    private List<AnalyticsEvent> GetCachedEventsData()
    {
        if (IsCacheEventsEmpty())
            return null;

        List<AnalyticsEvent> cachedAnalyticsEvents = new List<AnalyticsEvent>();

        if (_isWebGLApp)
        {
            string[] cachedEventsDataStrings = PlayerPrefs.GetString(EVENT_CACHE_PLAYER_PREFS_KEY).Trim(' ', '\n').Split('\n');

            for(int i = 0; i < cachedEventsDataStrings.Length; i++)
            {
                cachedAnalyticsEvents.AddRange(JsonConvert.DeserializeObject<AnalyticsEvent[]>(cachedEventsDataStrings[i]));
            }
        }
        else
        {            
            var lines = File.ReadLines(_eventCachePath);
            foreach (string dataLine in lines)
            {
                cachedAnalyticsEvents.AddRange(JsonConvert.DeserializeObject<AnalyticsEvent[]>(dataLine));
            }
        }

        return cachedAnalyticsEvents;
    }

    private void ClearCachedEventsData()
    {
        if (IsCacheEventsEmpty())
        {
            Debug.Log("Cache events data is empty!");
            return;

        }

        if (_isWebGLApp)
        {
            PlayerPrefs.DeleteKey(EVENT_CACHE_PLAYER_PREFS_KEY);
            PlayerPrefs.Save();
        }
        else
        {
            File.Delete(_eventCachePath);
        }

        Debug.Log("Cache events data cleared!");
    }

    private bool IsCacheEventsEmpty()
    {
        if (_isWebGLApp)
        {
            if (!PlayerPrefs.HasKey(EVENT_CACHE_PLAYER_PREFS_KEY))
                return true;
        }
        else
        {
            if (!File.Exists(_eventCachePath))
                return true;
        }

        return false;
    }

    private string TrackedEventsDataToJson()
    {
        return "{ \"events\":" + JsonConvert.SerializeObject(_events) + "}";
    }
}
