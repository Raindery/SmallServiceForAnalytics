using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public abstract class UIBase : MonoBehaviour
{
    #region Cache Component
    private Button _cacheButton;
    protected Button CacheButtonComponent
    {
        get
        {
            if (_cacheButton == null)
                _cacheButton = GetComponent<Button>();
            return _cacheButton;
        }
    }
    #endregion

    private void Awake()
    {
        CacheButtonComponent.onClick.AddListener(OnClickAction);
    }

    protected abstract void OnClickAction();

    protected abstract string GetFormatedDataString();
}
