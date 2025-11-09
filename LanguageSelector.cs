using UnityEngine;

public class LanguageSelector : MonoBehaviour
{
    public void SetEnglish() => LocalizationManager.Instance.SetLanguage(Language.English);
    public void SetSpanish() => LocalizationManager.Instance.SetLanguage(Language.Spanish);
    public void SetCatalan() => LocalizationManager.Instance.SetLanguage(Language.Catalan);
}
