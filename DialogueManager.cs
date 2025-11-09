using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace CaseFileNV51.Dialogue
{
    /// <summary>
    /// Sistema central de reproducci�n de di�logos
    /// Muestra subt�tulos con efecto typewriter y reproduce audio asociado
    /// </summary> 

    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField]
        [Tooltip("Panel que contiene los elementos de di�logo")]
        private GameObject dialoguePanel;

        [SerializeField]
        [Tooltip("Texto del hablante")]
        private TextMeshProUGUI speakerText;

        [SerializeField]
        [Tooltip("Texto del di�logo")]
        private TextMeshProUGUI dialogueText;

        [Header("Audio (Optional)")]
        [SerializeField]
        [Tooltip("AudioSource para reproducir voces")]
        private AudioSource voiceAudioSource;

        [Header("Typewriter Settings")]
        [SerializeField]
        [Tooltip("Velocidad por defecto para el efecto typewriter (puede ser sobreescrito por DialogueLine")]
        [Range(0.01f, 0.2f)]
        private float defaultLetterTime = 0.05f;

        [SerializeField]
        [Tooltip("Permitir saltar el efecto typewriter presionando una tecla")]
        private bool bAllowSkip = true;

        [SerializeField]
        [Tooltip("Tecla para saltar el typewriter")]
        private KeyCode skipKey = KeyCode.Space;

        // Estado interno
        private bool bIsShowingDialogue = false;
        private bool bIsTyping = false;
        private Coroutine currentDialogueCoroutine;

        // Referencias
        private Movement playerMovement;

        // Eventos (para que otros scripts se suscriban)
        public event Action OnDialogueStarted;
        public event Action OnDialogueEnded;
        public event Action<string> OnDialogueLineShown;

        #region Singleton Setup
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Buscar referencia al sistema de movimiento
            playerMovement = FindFirstObjectByType<Movement>();

            // Configurar AudioSource si existe
            if (voiceAudioSource != null)
            {
                voiceAudioSource.playOnAwake = false;
                voiceAudioSource.loop = false;
                voiceAudioSource.spatialBlend = 0f; // 2D audio (no espacial)
                voiceAudioSource.volume = 1f;
                voiceAudioSource.priority = 128;
                
                Debug.Log($"[DialogueManager] AudioSource configurado: Volume={voiceAudioSource.volume}, Spatial={voiceAudioSource.spatialBlend}");
            }
            else
            {
                Debug.LogWarning("[DialogueManager] ? No hay AudioSource asignado");
            }

            // Ocultar panel al inicio
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            // Suscribirse a cambios de idioma
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
        }

        private void OnDestroy()
        {
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Reproduce un di�logo completo desde un ScriptableObject
        /// </summary>
        public void PlayDialogue(DialogueData dialogueData)
        {
            if (dialogueData == null)
            {
                Debug.LogError("[DialogueManager] DialogueData es null");
            }

            if (!dialogueData.HasLines())
            {
                Debug.LogWarning($"[DialogueManager] {dialogueData.dialogueID} no tiene l�neas");
                return;
            }

            // Verificar si ya fue visto (one-shot)
            if (dialogueData.bIsOneShot && DialogueDatabase.Instance.HasSeen(dialogueData.dialogueID))
            {
                Debug.Log($"[DialogueManager] {dialogueData.dialogueID} ya fue visto (one-shot)");
                return;
            }

            // Detener di�logo anterior si existe
            if (currentDialogueCoroutine != null)
            {
                StopCoroutine(currentDialogueCoroutine);
            }

            // Iniciar nueva corutina
            currentDialogueCoroutine = StartCoroutine(ShowDialogueCoroutine(dialogueData));
        }

        /// <summary>
        /// Detiene el di�logo actual inmediatamente
        /// </summary>
        public void StopDialogue()
        {
            if (currentDialogueCoroutine != null)
            {
                StopCoroutine(currentDialogueCoroutine);
                currentDialogueCoroutine = null;
            }

            HideDialogueUI();
            RestorePlayerControl();
        }

        /// <summary>
        /// Verifica si se est� mostrando un d�alogo actualmente
        /// </summary>
        public bool IsShowingDialogue()
        {
            return bIsShowingDialogue;
        }
        #endregion

        #region Dialogue Playback
        /// <summary>
        /// Corutina principal que maneja el flujo del di�logo.
        /// </summary>
        private IEnumerator ShowDialogueCoroutine(DialogueData dialogueData)
        {
            bIsShowingDialogue = true;
            OnDialogueStarted?.Invoke();

            // Pausar controles si es necesario (ANTES del delay para prevenir movimiento)
            if (dialogueData.bPausePlayerMovement)
            {
                PausePlayerControl();
            }

            // Espera inicial ANTES de mostrar el panel
            if (dialogueData.initialDelay > 0)
            {
                yield return new WaitForSeconds(dialogueData.initialDelay);
            }

            // AHORA S� mostrar panel (despu�s del delay)
            ShowDialogueUI();

            // Reproducir cada l�nea
            Language currentLang = LocalizationManager.Instance.CurrentLanguage;

            foreach (DialogueLine line in dialogueData.lines)
            {
                yield return StartCoroutine(ShowLineCoroutine(line, currentLang));
            }

            // Marcar como visto
            DialogueDatabase.Instance.MarkAsSeen(dialogueData.dialogueID);

            // Completar fase autom�ticamente si es un di�logo principal
            if (DialogueSequenceManager.Instance != null && dialogueData.phase >= 0)
            {
                int currentPhase = DialogueSequenceManager.Instance.GetCurrentPhase();
                
                // Solo completar si es la fase actual (no fases anteriores)
                if (dialogueData.phase == currentPhase)
                {
                    DialogueSequenceManager.Instance.CompletePhase(dialogueData.phase);
                }
            }

            // Limpiar
            HideDialogueUI();
            RestorePlayerControl();

            bIsShowingDialogue = false;
            OnDialogueEnded?.Invoke();

            currentDialogueCoroutine = null;
        }

        /// <summary>
        /// Muestra una l�nea individual con efecto typewriter
        /// </summary>
        private IEnumerator ShowLineCoroutine(DialogueLine line, Language lang)
        {
            // Actualizar nombre del hablante
            if (speakerText != null)
            {
                speakerText.text = line.GetSpeaker(lang);
            }

            // Obtener texto completo
            string fullText = line.GetMessage(lang);
            dialogueText.text = "";

            // Reproducir audio si existe
            bool hasAudio = line.HasAudio() && voiceAudioSource != null;
            
            if (hasAudio)
            {
                voiceAudioSource.clip = line.audioClip;
                voiceAudioSource.Play();
            }

            // Efecto typewriter
            bIsTyping = true;
            float letterDelay = line.letterTime > 0 ? line.letterTime : defaultLetterTime;

            for (int i = 0; i < fullText.Length; i++)
            {
                dialogueText.text += fullText[i];

                // Permitir saltar
                if (bAllowSkip && Input.GetKeyDown(skipKey))
                {
                    dialogueText.text = fullText;
                    
                    // Si hay audio y se salta, tambi�n saltar el audio
                    if (hasAudio && voiceAudioSource.isPlaying)
                    {
                        voiceAudioSource.Stop();
                    }
                    
                    break;
                }

                yield return new WaitForSeconds(letterDelay);
            }

            bIsTyping = false;

            // Notificar que se mostr� esta l�nea
            OnDialogueLineShown?.Invoke(line.lineID);

            // Si hay audio, esperar a que termine
            if (hasAudio && voiceAudioSource.isPlaying)
            {
                yield return new WaitWhile(() => voiceAudioSource.isPlaying);

                // Peque�a pausa despu�s del audio
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                // Sin audio, usar el tiempo configurado
                yield return new WaitForSeconds(line.displayTime);
            }

            // Esperar input del jugador o timeout autonático
            float autoCloseDelay = 0.5f; // Segundos extra para cerrar el diálogo automáticamente
            float elapsed = 0.0f;
            bool inputReceived = false;

            while (elapsed < autoCloseDelay && !inputReceived)
            {
                if (Input.anyKeyDown)
                {
                    inputReceived = true;
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Debug opcional para ver cómo se cerró
            if (inputReceived)
            {
                Debug.Log("[DialogueManager] Diálogo cerrado por input del jugador");
            }
            else
            {
                Debug.Log("[DialogueManager] Diálogo cerrado automáticamente (timeout)");
            }
        }
        #endregion

        #region UI Control
        private void ShowDialogueUI()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }
        }

        private void HideDialogueUI()
        {
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (speakerText != null)
            {
                speakerText.text = "";
            }

            if (dialogueText != null)
            {
                dialogueText.text = "";
            }
        }
        #endregion

        #region Player Control
        private void PausePlayerControl()
        {
            if (playerMovement != null)
            {
                playerMovement.bCanMove = false;
                playerMovement.bCanRotate = false;
            }

            // Opcional: desbloquear cursor
            // Cursor.lockState = CursorLockMode.None;
        }

        private void RestorePlayerControl()
        {
            if (playerMovement != null)
            {
                playerMovement.bCanMove = true;
                playerMovement.bCanRotate = true;
            }

            // Opcional: bloquear cursor de nuevo
            // Cursor.lockState = CursorLockMode.Locked;
        }
        #endregion

        #region Localization Support
        /// <summary>
        /// Callback cuando el idioma cambia durante un di�logo activo.
        /// </summary>
        private void OnLanguageChanged(Language newLang)
        {
            // Si hay un di�logo activo, no hacer nada (esperar a que termine)
            // En el futuro podr�as implementar cambio din�mico si lo necesitas
            if (bIsShowingDialogue)
            {
                Debug.Log("[DialogueManager] Idioma cambiado durante di�logo (se aplicar� en el pr�ximo)");
            }
        }
        #endregion

        #region Debug
        [ContextMenu("Test: Play Phase 0 Intro")]
        private void TestPlayPhase0Intro()
        {
            // Este m�todo permite probar desde el Inspector
            // Necesitar�s asignar manualmente el DialogueData en el c�digo o inspector
            Debug.Log("[DialogueManager] Busca 'Dialogue_Phase0_Intro' y as�gnalo para probar");
        }
        #endregion
    }
}
