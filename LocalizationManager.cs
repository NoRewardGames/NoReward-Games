using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    public Language CurrentLanguage { get; private set; } = Language.English;

    public delegate void OnLanguageChangedDelegate(Language newLang);
    public event OnLanguageChangedDelegate OnLanguageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("Language"))
        {
            CurrentLanguage = (Language)PlayerPrefs.GetInt("Language");
        }
    }

    public void SetLanguage(Language lang)
    {
        if (CurrentLanguage == lang) return;

        CurrentLanguage = lang;
        PlayerPrefs.SetInt("Language", (int)lang);
        OnLanguageChanged?.Invoke(lang);
    }
}
