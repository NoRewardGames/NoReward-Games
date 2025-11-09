using UnityEngine;

namespace CaseFileNV51.Dialogue
{
    /// <summary>
    /// Representa una línea individual de diálogo con soporte multiidioma y audio opcional.
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        [Header("Identification")]
        [Tooltip("Unique ID for this line (example: 'phase0_intro_line1')")]
        public string lineID;

        [Header("Multilingual Content")]
        [Tooltip("Speaker name in each language")]
        public SerializableTranslations speakerNames = new SerializableTranslations();

        [Tooltip("Dialogue text in each language")]
        public SerializableTranslations translations = new SerializableTranslations();

        [Header("Audio")]
        [Tooltip("Audio clip for this line (leave empty if none)")]
        public AudioClip audioClip;

        [Header("Timing")]
        [Tooltip("Time taken to type each letter (typewriter effect)")]
        [Range(0.01f, 0.2f)]
        public float letterTime = 0.05f;

        [Tooltip("Wait time after the line is fully displayed")]
        [Range(0.5f, 5.0f)]
        public float displayTime = 2.0f;

        // Métodos de acceso
        public string GetSpeaker(Language lang)
        {
            return speakerNames.GetText(lang);
        }

        public string GetMessage(Language lang)
        {
            return translations.GetText(lang);
        }

        public bool HasAudio()
        {
            return audioClip != null;
        }
    }
}
