using UnityEngine;
using System.Collections.Generic;

namespace CaseFileNV51.Dialogue
{
    // <summary>
    // Clase que representa los datos completos de un diálogo, incluyendo múltiples líneas y metadatos.
    // </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "NV-51/Dialogue Data", order = 1)]
    public class DialogueData : ScriptableObject
    {
        [Header("Dialogue Identification")]
        [Tooltip("Unique ID for this dialogue (example: 'phase0_intro', 'get_gas_mission')")]
        public string dialogueID;

        [Tooltip("GDD phase to which it belongs")]
        [Range(-1, 12)]
        public int phase = 0;

        [Header("Prerequisites (Optional)")]
        [Tooltip("Dialogues that should have been seen before (leave blank if none)")]
        public List<string> requiredDialogueIDs = new List<string>();

        [Tooltip("Missions that must be completed (leave blank if none)")]
        public List<string> requiredMissions = new List<string>();

        [Tooltip("Items the player must have (leave blank if none)")]
        public List<string> requiredItems = new List<string>();

        [Header("Dialogue Lines")]
        [Tooltip("Lines that make up this dialogue / monologue")]
        public List<DialogueLine> lines = new List<DialogueLine>();

        [Header("Playback Settings")]
        [Tooltip("If true, this dialogue can only be seen once")]
        public bool bIsOneShot = true;

        [Tooltip("Pause player controls during the dialogue")]
        public bool bPausePlayerMovement = true;

        [Tooltip("Wait time before displaying first message (in seconds) ")]
        [Range(0.0f, 5.0f)]
        public float initialDelay = 0f;

        // Métodos auxiliares
        public bool HasLines()
        {
            return lines != null && lines.Count > 0;
        }

        public int GetLineCount()
        {
            return lines?.Count ?? 0;
        }

        public DialogueLine GetLine(int index)
        {
            if (lines == null || index < 0 || index >= lines.Count)
                return null;

            return lines[index];
        }
    }
}
