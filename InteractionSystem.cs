using System.Data.Common;
using TMPro;
using UnityEditor;
using UnityEngine;

public class InteractionSystem : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private Camera playerCamera;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactionPromptText;
    private string currentPromptText = ""; // Variable para recordar el texto actual

    private void Update()
    {
        UpdateInteractionPrompt();

        if (Input.GetKeyDown(KeyCode.F) && !VehicleController.Instance.bIsDriving && !Movement.Instance.bIsInteracting)
        {
            TryInteract();
        }
        else if (Input.GetKeyDown(KeyCode.F) && VehicleController.Instance.bIsDriving)
        {
            VehicleController.Instance.ExitVehicle();
        }

        // Solo permitir soltar items si NO est�s conduciendo
        if (Input.GetKeyDown(KeyCode.G) && !VehicleController.Instance.bIsDriving)
        {
            Vector3 throwForce = playerCamera.transform.forward * 6.0f;

            // PRIORIDAD 1
            if (Inventory.Instance.equippedFlashlight != null)
            {
                Inventory.Instance.DropFlashlight(throwForce);
            }

            // PRIORIDAD 2
            else if (Inventory.Instance.currentHeldItem != null)
            {
                Inventory.Instance.DropCurrentItem(throwForce);
            }
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.red, 1f);
        
        // Detectar TODO (sin filtro de layer)
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact();
            }
            // Si no tiene IInteractable, simplemente no hace nada (el obst�culo bloque�)
        }
    }

    private void UpdateInteractionPrompt()
    {
        if (VehicleController.Instance.bIsDriving || Movement.Instance.bIsInteracting)
        {
            if (interactionPromptText.enabled)
            {
                interactionPromptText.enabled = false;
                currentPromptText = "";
            }
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Detectar TODO (sin filtro de layer)
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                string promptText = interactable.GetInteractionPrompt();
                
                // Solo mostrar si el prompt NO está vacío
                if (!string.IsNullOrEmpty(promptText))
                {
                    string newPrompt = $"[F] {promptText}";

                    if (currentPromptText != newPrompt)
                    {
                        interactionPromptText.text = newPrompt;
                        currentPromptText = newPrompt;
                    }

                    if (!interactionPromptText.enabled)
                        interactionPromptText.enabled = true;

                    return; // ← IMPORTANTE: Salir aquí si hay prompt válido
                }
                else
                {
                    // Si el prompt está vacío, continuar para ocultar
                }

                return;
            }
        }

        // OCULTAR PROMPT (se ejecuta si NO hay raycast válido O si el prompt está vacío)
        if (interactionPromptText.enabled)
        {
            interactionPromptText.enabled = false;
            currentPromptText = "";
        }
    }
}


