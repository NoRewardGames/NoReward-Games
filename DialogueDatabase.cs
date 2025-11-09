using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace CaseFileNV51.Dialogue
{
    /// <summary>
    /// Sistema centralizado que lleva registro de todos los diálogos vistos por el jugador
    /// Soporta checkpoints manuales en puntos narrativos específicos (Fase 11, etc)
    /// Patrón Singleton para acceso global
    /// </summary>

    public class DialogueDatabase : MonoBehaviour
    {
        public static DialogueDatabase Instance { get; private set; }

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Shows all dialogs viewed in the inspector (read-only)")]
        private List<string> debugSeenDialogues = new List<string>();

        [SerializeField]
        [Tooltip("Last saved checkpoint (empty = no checkpoint)")]
        private string debugLastCheckpoint = "";

        // HashSet para búsqueda rápida (0(1))
        private HashSet<string> seenDialogues = new HashSet<string>();

        // Checkpoint actual
        private string currentCheckpoint = "";

        // Claves para guardar en PlayerPrefs
        private const string SAVE_KEY_DIALOGUES = "NV51_SeenDialogues";
        private const string SAVE_KEY_CHECKPOINT = "NV51_LastCheckpoint";

        #region Singleton Setup
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
        #endregion

        #region Registro de diálogos vistos
        /// <summary>
        /// Marca un díalogo como visto (NO guarda automáticamente)
        /// </summary>
        public void MarkAsSeen(string dialogueID)
        {
            if (string.IsNullOrEmpty(dialogueID))
            {
                Debug.LogWarning("[DialogueDatabase] Attempted to mark dialogue with empty ID");
                return;
            }

            if (seenDialogues.Add(dialogueID))
            {
                Debug.Log($"[DialogueDatabase] Seen dialogue: {dialogueID}");
                UpdateDebugList();
                // NO guarda automáticamente
            }
        }

        /// <summary>
        /// Verifica si un diálogo ya fue visto
        /// </summary>
        public bool HasSeen(string dialogueID)
        {
            return seenDialogues.Contains(dialogueID);
        }

        /// <summary>
        /// Verifica si TODOS los diálogos de una lista han sido vistos
        /// </summary>
        public bool HasSeenAll(List<string> dialogueIDs)
        {
            if (dialogueIDs == null || dialogueIDs.Count == 0)
                return true;

            foreach (string id in dialogueIDs)
            {
                if (!seenDialogues.Contains(id))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Limpia todos los diálogos (para reiniciar partida)
        /// </summary>
        public void ClearAllSeen()
        {
            seenDialogues.Clear();
            currentCheckpoint = "";
            UpdateDebugList();
            Debug.Log("[DialogueDatabase] Todos los diálogos vistos han sido limpiados");
        }
        #endregion

        #region Sistema de checkpoints
        /// <summary>
        /// Crea un checkpoint manual en un punto narrativo específico
        /// EJEMPLO: SaveCheckpoint("phase11_module_entrance")
        /// </summary>
        public void SaveCheckpoint(string checkpointID)
        {
            currentCheckpoint = checkpointID;
            debugLastCheckpoint = checkpointID;

            // Guardar diálogos vistos + checkpoint
            SaveToPlayerPrefs();

            Debug.Log($"[DialogueDatabase] CHECKPOINT GUARDADO: {checkpointID}");
        }

        /// <summary>
        /// Carga el último checkpoint guardado
        /// Devuelve TRUE si había un checkpoint, false si no
        /// </summary>
        public bool LoadCheckpoint()
        {
            LoadFromPlayerPrefs();

            if (!string.IsNullOrEmpty(currentCheckpoint))
            {
                Debug.Log($"[DialogueDatabase] Checkpoint cargado: {currentCheckpoint}");
                return true;
            }
            else
            {
                Debug.Log("[DialogueDatabase] No hay checkpoint guardado");
                return false;
            }
        }

        /// <summary>
        /// Devuelve el ID del último checkpoint guardado
        /// </summary>
        public string GetLastCheckpoint()
        {
            return currentCheckpoint;
        }

        /// <summary>
        /// Verifica si existe un checkpoint guardado
        /// </summary>
        public bool HasCheckpoint()
        {
            return !string.IsNullOrEmpty(currentCheckpoint);
        }
        #endregion

        #region Persistencia (PlayerPrefs)
        /// <summary>
        /// Guarda los diálogos vistos + checkpoint en PlayerPrefs
        /// </summary>
        private void SaveToPlayerPrefs()
        {
            // Guardar diálogos vistos
            string dialoguesData = string.Join(",", seenDialogues);
            PlayerPrefs.SetString(SAVE_KEY_DIALOGUES, dialoguesData);

            // Guardar checkpoint
            PlayerPrefs.SetString(SAVE_KEY_CHECKPOINT, currentCheckpoint);

            PlayerPrefs.Save();

            Debug.Log($"[DialogueDatabase] Datos guardados ({seenDialogues.Count} diálogos, checkpoint: {currentCheckpoint})");
        }

        /// <summary>
        /// Carga los diálogos vistos + checkpoint desde PlayerPrefs
        /// </summary>
        private void LoadFromPlayerPrefs()
        {
            // Cargar diálogos vistos
            string dialoguesData = PlayerPrefs.GetString(SAVE_KEY_DIALOGUES, "");
            if (!string.IsNullOrEmpty(dialoguesData))
            {
                string[] ids = dialoguesData.Split(',');
                foreach (string id in ids)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        seenDialogues.Add(id);
                    }
                }
            }

            // Cargar checkpoint
            currentCheckpoint = PlayerPrefs.GetString(SAVE_KEY_CHECKPOINT, "");
            debugLastCheckpoint = currentCheckpoint;

            UpdateDebugList();

            Debug.Log($"[DialogueDatabase] Datos cargados: {seenDialogues.Count} diálogos, checkpoint: {currentCheckpoint}");
        }
        #endregion

        #region Gestión de partidas
        /// <summary>
        /// Inicia una nueva partida (limpia datos y borra PlayerPrefs)
        /// </summary>
        public void StartNewGame()
        {
            seenDialogues.Clear();
            currentCheckpoint = "";
            UpdateDebugList();
            Debug.Log("[DialogueDatabase] Nueva partida iniciada");
        }

        /// <summary>
        /// Borra PERMANENTEMENTE todos los datos guardados
        /// </summary>
        public void DeleteSaveData()
        {
            PlayerPrefs.DeleteKey(SAVE_KEY_DIALOGUES);
            PlayerPrefs.DeleteKey(SAVE_KEY_CHECKPOINT);
            PlayerPrefs.Save();

            seenDialogues.Clear();
            currentCheckpoint = "";
            debugLastCheckpoint = "";
            UpdateDebugList();

            Debug.Log("[DialogueDatabase] Datos guardados eliminados permanentemente");
        }
        #endregion

        #region Debug
        /// <summary>
        /// Actualiza la lista de debug visible en el inspector
        /// </summary>
        private void UpdateDebugList()
        {
            debugSeenDialogues.Clear();
            debugSeenDialogues.AddRange(seenDialogues);
            debugSeenDialogues.Sort();
        }

        /// <summary>
        /// Método de debug para imprimir todos los diálogos vistos
        /// </summary>
        [ContextMenu("Print Seen Dialogues")]
        public void PrintSeenDialogues()
        {
            Debug.Log($"=== DIALOGUES SEEN ({seenDialogues.Count}) ===");
            foreach (string id in seenDialogues)
            {
                Debug.Log($" {id}");
            }
            Debug.Log($"=== CHECKPOINT: {currentCheckpoint} ===");
        }

        /// </summary>
        /// Método de debug para limpiar datos guardados
        /// </summary>
        [ContextMenu("DEBUG: Delete All Save Data")]
        public void DebugDeleteSaveData()
        {
            DeleteSaveData();
        }
        #endregion
    }
}

