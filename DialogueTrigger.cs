using UnityEngine;
using CaseFileNV51.Dialogue;

namespace CaseFileNV51.Dialogue
{
    /// <summary>
    /// Sistema de triggers para activar di�logos autom�ticamente
    /// Soporta m�ltiples modos de activaci�n y validaci�n de prerequisitos
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DialogueTrigger : MonoBehaviour
    {
        [Header("Dialogue Configuration")]
        [SerializeField]
        [Tooltip("Di�logo que se reproducir� al activar este trigger")]
        private DialogueData dialogueToPlay;

        [SerializeField]
        [Tooltip("Fase del GDD a la que pertenece este trigger (0-12). Si es -1, no valida fase.")]
        [Range(-1, 12)]
        private int requiredPhase = -1;

        [Header("Trigger Settings")]
        [SerializeField]
        [Tooltip("Modo de activaci�n del trigger")]
        private TriggerMode activationMode = TriggerMode.OnEnter;

        [SerializeField]
        [Tooltip("Tag requerido para activar (normalmente 'Player')")]
        private string requiredTag = "Player";

        [SerializeField]
        [Tooltip("Si es true, el trigger se desactiva despu�s de usarse")]
        private bool disableAfterUse = true;

        [Header("Prerequisites (Opcional)")]
        [SerializeField]
        [Tooltip("Di�logos que deben haberse visto antes")]
        private string[] requiredDialogues;

        [SerializeField]
        [Tooltip("Misiones || tareas que deben estar completadas")]
        private string[] requiredMissions;

        [SerializeField]
        [Tooltip("Items que debe tener el jugador")]
        private string[] requiredItems;

        [Header("Visual Feedback")]
        [SerializeField]
        [Tooltip("Color del Gizmo en el editor")]
        private Color gizmoColor = new Color (0, 1, 0, 0.3f); // Verde transparente

        [SerializeField]
        [Tooltip("Mostrar �rea de trigger en la escena")]
        private bool bShowGizmo = true;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Mostrar logs detallados de activaci�n")]
        private bool debugMode = false;

        // Estado interno
        private bool bHasBeenTriggered = false;
        private Collider triggerCollider;

        /// <summary>
        /// Modos de activaci�n del trigger
        /// </summary>
        public enum TriggerMode
        {
            OnEnter,      // Se activa al entrar en el trigger
            OnStay,       // Se activa mientras est� dentro (puede ser molesto)
            OnExit,       // Se activa al salir del trigger
            Manual        // Solo se activa llamando PlayDialogue() manualmente
        }

        #region Unity Lifecycle
        private void Awake()
        {
            // Configurar collider como trigger
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
            }
            else
            {
                Debug.LogError($"[DialogueTrigger] {gameObject.name} no tiene Collider. Agregando BoxCollider autom�ticamente.");
                triggerCollider = gameObject.AddComponent<BoxCollider>();
                triggerCollider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (activationMode == TriggerMode.OnEnter)
            {
                TryActivate(other);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (activationMode == TriggerMode.OnStay)
            {
                TryActivate(other);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (activationMode == TriggerMode.OnExit)
            {
                TryActivate(other);
            }
        }
        #endregion

        #region Activation Logic
        /// <summary>
        /// Intenta activar el di�logo si se cumplen las condiciones
        /// </summary>
        private void TryActivate(Collider other)
        {
            // 1. Verificar si ya fue usado
            if (bHasBeenTriggered && disableAfterUse)
            {
                if (debugMode) Debug.Log($"[DialogueTrigger] {gameObject.name} ya fue usado (one-shot)");
                return;
            }

            // 2. Verificar tag
            if (!other.CompareTag(requiredTag))
            {
                if (debugMode) Debug.Log($"[DialogueTrigger] {gameObject.name} ignorado: tag incorrecto ({other.tag} != {requiredTag})");
                return;
            }

            // 3. Verificar que hay di�logo asignado
            if (dialogueToPlay == null)
            {
                Debug.LogWarning($"[DialogueTrigger] {gameObject.name} no tiene DialogueData asignado");
                return;
            }

            // 4. Verificar prerequisites
            if (!CheckPrerequisites())
            {
                if (debugMode) Debug.Log($"[DialogueTrigger] {gameObject.name} no cumple prerequisites");
                return;
            }

            // 5. Verificar que no hay otro di�logo activo
            if (DialogueManager.Instance.IsShowingDialogue())
            {
                if (debugMode) Debug.Log($"[DialogueTrigger] {gameObject.name} esperando: ya hay un di�logo activo");
                return;
            }

            // TODO OK - Activar di�logo
            ActivateDialogue();
        }

        /// <summary>
        /// Verifica si se cumplen todos los prerequisitos
        /// </summary>
        private bool CheckPrerequisites()
        {
            // NUEVO: Verificar fase del sequence manager
            if (requiredPhase >= 0 && DialogueSequenceManager.Instance != null)
            {
                if (!DialogueSequenceManager.Instance.CanAccessPhase(requiredPhase))
                {
                    if (debugMode) 
                    {
                        Debug.Log($"[DialogueTrigger] No se puede acceder a fase {requiredPhase}. Fase actual: {DialogueSequenceManager.Instance.GetCurrentPhase()}");
                    }
                    return false;
                }
            }

            // Verificar di�logos vistos
            if (requiredDialogues != null && requiredDialogues.Length > 0)
            {
                foreach (string dialogueId in requiredDialogues)
                {
                    if (!DialogueDatabase.Instance.HasSeen(dialogueId))
                    {
                        if (debugMode) Debug.Log($"[DialogueTrigger] Falta ver di�logo: {dialogueId}");
                        return false;
                    }
                }
            }

            // Verificar misiones completadas
            if (requiredMissions != null && requiredMissions.Length > 0)
            {
                foreach (string missionId in requiredMissions)
                {
                    if (!MissionManager.Instance.IsMissionCompleted(missionId))
                    {
                        if (debugMode) Debug.Log($"[DialogueTrigger] Falta completar misi�n: {missionId}");
                        return false;
                    }
                }
            }

            // Verificar items en inventario
            if (requiredItems != null && requiredItems.Length > 0)
            {
                foreach (string itemId in requiredItems)
                {
                    if (!Inventory.Instance.HasItem(itemId))
                    {
                        if (debugMode) Debug.Log($"[DialogueTrigger] Falta item: {itemId}");
                        return false;
                    }
                }
            }

            return true; // Todos los prerequisites cumplidos
        }

        /// <summary>
        /// Activa el di�logo y marca el trigger como usado
        /// </summary>
        private void ActivateDialogue()
        {
            if (debugMode || true) // Siempre loguear activaciones importantes
            {
                Debug.Log($"[DialogueTrigger] ACTIVADO: {gameObject.name} {dialogueToPlay.dialogueID}");
            }

            // Reproducir di�logo
            DialogueManager.Instance.PlayDialogue(dialogueToPlay);

            // Marcar como usado
            bHasBeenTriggered = true;

            // Desactivar si es one-shot
            if (disableAfterUse)
            {
                triggerCollider.enabled = false;
                if (debugMode) Debug.Log($"[DialogueTrigger] {gameObject.name} desactivado (one-shot)");
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Activa el di�logo manualmente (ignora modo de activaci�n)
        /// </summary>
        public void PlayDialogueManual()
        {
            if (dialogueToPlay == null)
            {
                Debug.LogWarning($"[DialogueTrigger] {gameObject.name} no tiene DialogueData asignado");
                return;
            }

            if (CheckPrerequisites())
            {
                ActivateDialogue();
            }
            else
            {
                Debug.LogWarning($"[DialogueTrigger] {gameObject.name} no cumple prerequisites");
            }
        }

        /// <summary>
        /// Resetea el trigger para que pueda activarse de nuevo
        /// </summary>
        public void ResetTrigger()
        {
            bHasBeenTriggered = false;
            triggerCollider.enabled = true;
            if (debugMode) Debug.Log($"[DialogueTrigger] {gameObject.name} reseteado");
        }

        /// <summary>
        /// Verifica si el trigger ya fue usado
        /// </summary>
        public bool HasBeenTriggered()
        {
            return bHasBeenTriggered;
        }
        #endregion

        #region Gizmos (Visual en Scene View)
        private void OnDrawGizmos()
        {
            if (!bShowGizmo) return;

            Collider col = GetComponent<Collider>();
            if (col == null) return;

            // Color seg�n estado
            Color finalColor = gizmoColor;
            if (bHasBeenTriggered && disableAfterUse)
            {
                finalColor = new Color(0.5f, 0.5f, 0.5f, 0.2f); // Gris si ya se us�
            }

            Gizmos.color = finalColor;

            // Dibujar seg�n tipo de collider
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius);
                Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
            }
            else if (col is CapsuleCollider capsule)
            {
                // Unity no tiene Gizmos.DrawCapsule, usar esfera como aproximaci�n
                Gizmos.DrawWireSphere(transform.position + capsule.center, capsule.radius);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!bShowGizmo) return;

            // Dibujar nombre del di�logo cuando est� seleccionado
            if (dialogueToPlay != null)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 2f,
                    $"Trigger: {dialogueToPlay.dialogueID}",
                    new GUIStyle()
                    {
                        normal = new GUIStyleState() { textColor = Color.yellow },
                        fontSize = 12,
                        fontStyle = FontStyle.Bold
                    }
                );
#endif
            }
        }
        #endregion

        #region Context Menu (Debug)
        [ContextMenu("Test: Play Dialogue")]
        private void TestPlayDialogue()
        {
            if (Application.isPlaying)
            {
                PlayDialogueManual();
            }
            else
            {
                Debug.LogWarning("[DialogueTrigger] Solo se puede probar en Play Mode");
            }
        }

        [ContextMenu("Reset Trigger")]
        private void TestResetTrigger()
        {
            ResetTrigger();
        }

        [ContextMenu("Log Prerequisites Status")]
        private void LogPrerequisitesStatus()
        {
            Debug.Log($"=== PREREQUISITES STATUS: {gameObject.name} ===");

            if (requiredDialogues != null && requiredDialogues.Length > 0)
            {
                Debug.Log("Required Dialogues:");
                foreach (string id in requiredDialogues)
                {
                    bool seen = DialogueDatabase.Instance?.HasSeen(id) ?? false;
                    Debug.Log($"  {(seen ? "T" : "F")} {id}");
                }
            }

            if (requiredMissions != null && requiredMissions.Length > 0)
            {
                Debug.Log("Required Missions:");
                foreach (string id in requiredMissions)
                {
                    bool completed = MissionManager.Instance?.IsMissionCompleted(id) ?? false;
                    Debug.Log($"  {(completed ? "T" : "F")} {id}");
                }
            }

            if (requiredItems != null && requiredItems.Length > 0)
            {
                Debug.Log("Required Items:");
                foreach (string id in requiredItems)
                {
                    bool has = Inventory.Instance?.HasItem(id) ?? false;
                    Debug.Log($"  {(has ? "T" : "F")} {id}");
                }
            }
        }
        #endregion
    }
}

