using UnityEngine;
using System;
using System.Collections.Generic;

namespace CaseFileNV51.Dialogue
{
    /// <summary>
    /// Clase auxiliar para serializar traducciones multiidioma en Unity
    /// Unity no puede serializar Dictionary directamente, así que usamos listas paralelas
    /// </summary>
    [System.Serializable]
    public class SerializableTranslations
    {
        [SerializeField]
        private List<Language> languages = new List<Language>();

        [SerializeField]
        private List<string> texts = new List<string>();

        // Constructor vacío
        public SerializableTranslations()
        {
            // Inicializar con los 3 idiomas por defecto (o los que sean)
            languages.Add(Language.English);
            languages.Add(Language.Spanish);
            languages.Add(Language.Catalan);

            texts.Add("");
            texts.Add("");
            texts.Add("");
        }

        // Método para obtener texto por idioma
        public string GetText(Language lang)
        {
            int index = languages.IndexOf(lang);
            if (index >= 0 && index < texts.Count)
                return texts[index];

            // Fallback a inglés si no se encuentra
            index = languages.IndexOf(Language.English);
            if (index >= 0 && index < texts.Count)
                return texts[index];

            return "[MISSING TRANSLATION]";
        }

        // Método para establecer texto
        public void SetText(Language lang, string text)
        {
            int index = languages.IndexOf(lang);
            if (index >= 0 && index < texts.Count)
            {
                texts[index] = text;
            }
            else
            {
                // Si el idioma no existe, agregarlo
                languages.Add(lang);
                texts.Add(text);
            }
        }

        // Verificar si tiene traducción para un idioma
        public bool HasTranslation(Language lang)
        {
            int index = languages.IndexOf(lang);
            return index >= 0 && index < texts.Count && !string.IsNullOrEmpty(texts[index]);
        }

        // Convertir a Dictionary normal 
        public Dictionary<Language, string> ToDictionary()
        {
            Dictionary<Language, string> dict = new Dictionary<Language, string>();
            for (int i = 0; i < languages.Count && i < texts.Count; i++)
            {
                dict[languages[i]] = texts[i];
            }

            return dict;
        }

        // Crear desde Dictionary
        public static SerializableTranslations FromDictionary(Dictionary<Language, string> dict)
        {
            SerializableTranslations translations = new SerializableTranslations();
            translations.languages.Clear();
            translations.texts.Clear();

            foreach (var kvp in dict)
            {
                translations.languages.Add(kvp.Key);
                translations.texts.Add(kvp.Value);
            }

            return translations;
        }
    }
}

