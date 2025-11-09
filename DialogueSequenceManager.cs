using System.Collections.Generic;
using UnityEngine;
using CaseFileNV51.Dialogue;

namespace CaseFileNV51.Dialogue
{
    /// <summary>
    /// Controla la secuencia lineal de fases del GDD (0-12).
    /// Previene saltos de fase y valida progresión narrativa.
    /// </summary>
    public class DialogueSequenceManager : MonoBehaviour
    {
        public static DialogueSequenceManager Instance { get; private set; }

        [Header("Phase Configuration")]
        [SerializeField]
        [Tooltip("Fase actual del jugador (0-12 según GDD)")]
        private int currentPhase = 0;

        [SerializeField]
        [Tooltip("Lista de diálogos principales de cada fase (en orden)")]
        private DialogueData[] mainPhaseDialogues = new DialogueData[13]; // 0-12 = 13 fases

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Mostrar logs de progresión de fases")]
        private bool debugMode = true;

        [SerializeField]
        [Tooltip("Fases completadas (solo lectura)")]
        private List<int> completedPhases = new List<int>();

        // Eventos
        public event System.Action<int> OnPhaseCompleted;
        public event System.Action<int> OnPhaseStarted;

        #region Singleton
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

        #region Phase Management
        /// <summary>
        /// Verifica si el jugador puede acceder a una fase específica.
        /// </summary>
        public bool CanAccessPhase(int phase)
        {
            // Fase 0 siempre es accesible (intro)
            if (phase == 0) return true;

            // Solo puedes acceder a la fase actual o anteriores
            return phase <= currentPhase;
        }

        /// <summary>
        /// Verifica si una fase específica ya fue completada.
        /// </summary>
        public bool IsPhaseCompleted(int phase)
        {
            return completedPhases.Contains(phase);
        }

        /// <summary>
        /// Marca una fase como completada y avanza a la siguiente.
        /// </summary>
        public void CompletePhase(int phase)
        {
            if (phase != currentPhase)
            {
                Debug.LogWarning($"[SequenceManager] Intentando completar fase {phase} pero la actual es {currentPhase}");
                return;
            }

            if (!completedPhases.Contains(phase))
            {
                completedPhases.Add(phase);
                
                if (debugMode)
                {
                    Debug.Log($"[SequenceManager] FASE {phase} COMPLETADA");
                }

                OnPhaseCompleted?.Invoke(phase);
            }

            // Avanzar a la siguiente fase
            if (currentPhase < 12) // Máximo fase 12
            {
                currentPhase++;
                
                if (debugMode)
                {
                    Debug.Log($"[SequenceManager] Avanzando a FASE {currentPhase}");
                }

                OnPhaseStarted?.Invoke(currentPhase);
            }
            else
            {
                if (debugMode)
                {
                    Debug.Log("[SequenceManager] JUEGO COMPLETADO (Fase 12)");
                }
            }
        }

        /// <summary>
        /// Obtiene el DialogueData principal de una fase específica.
        /// </summary>
        public DialogueData GetPhaseDialogue(int phase)
        {
            if (phase < 0 || phase >= mainPhaseDialogues.Length)
            {
                Debug.LogError($"[SequenceManager] Fase {phase} fuera de rango (0-12)");
                return null;
            }

            return mainPhaseDialogues[phase];
        }

        /// <summary>
        /// Devuelve la fase actual.
        /// </summary>
        public int GetCurrentPhase()
        {
            return currentPhase;
        }

        /// <summary>
        /// Reinicia la progresión a la Fase 0 (para New Game).
        /// </summary>
        public void ResetProgression()
        {
            currentPhase = 0;
            completedPhases.Clear();
            
            if (debugMode)
            {
                Debug.Log("[SequenceManager] Progresión reiniciada");
            }
        }
        #endregion

        #region Context Menu (Debug)
        [ContextMenu("Force Advance to Next Phase")]
        private void DebugAdvancePhase()
        {
            CompletePhase(currentPhase);
        }

        [ContextMenu("Reset Progression")]
        private void DebugResetProgression()
        {
            ResetProgression();
        }

        [ContextMenu("Log Current Status")]
        private void DebugLogStatus()
        {
            Debug.Log($"=== SEQUENCE MANAGER STATUS ===");
            Debug.Log($"Current Phase: {currentPhase}");
            Debug.Log($"Completed Phases: [{string.Join(", ", completedPhases)}]");
            Debug.Log($"Can access Phase 5: {CanAccessPhase(5)}");
        }

        [ContextMenu("Complete Current Phase")]
        private void DebugCompleteCurrentPhase()
        {
            CompletePhase(currentPhase);
        }
        #endregion
    }
}
