using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIAlwaysSelect : MonoBehaviour
{
    private EventSystem currentEventSystem;
    private GameObject currentlySelected;

    private void Awake()
    {
    // oeOnLoad(gameObject);
    }

    private void Start()
    {
        currentEventSystem = EventSystem.current;
        currentlySelected = currentEventSystem.currentSelectedGameObject;
    }

    private void Update()
    {
        // Verifica se o EventSystem está disponível
        if (currentEventSystem == null)
        {
            currentEventSystem = EventSystem.current;
            if (currentEventSystem == null)
                return;
        }

        //Check if the last known selected GameObject has changed since
        //the last frame
        if (currentEventSystem.currentSelectedGameObject != null &&
            currentlySelected != currentEventSystem.currentSelectedGameObject)
        {
            currentlySelected = currentEventSystem.currentSelectedGameObject;
        }

        // The currentSelectedGameObject will be null when you click with your
        // anywhere on the screen on a non-Selectable GameObject.
        if (currentEventSystem.currentSelectedGameObject == null)
        {
            // If this happens simply re-select the last known selected GameObject.
            if (currentlySelected != null)
            {
                // Verifica se o objeto ainda existe e está ativo na hierarquia
                // (importante quando menus são desabilitados durante cutscenes)
                if (currentlySelected.activeInHierarchy)
                {
                    var selectable = currentlySelected.GetComponent<Selectable>();
                    if (selectable != null && selectable.interactable)
                    {
                        selectable.Select();
                    }
                    else
                    {
                        // Se o objeto não é mais selecionável, limpa a referência
                        currentlySelected = null;
                    }
                }
                else
                {
                    // Se o objeto foi desabilitado (ex: durante cutscene), limpa a referência
                    currentlySelected = null;
                }
            }
            else
            {
                // If there is none, select the firstSelectedGameObject
                // (which can be setup inthe EventSystem component).
                if (currentEventSystem.firstSelectedGameObject != null)
                {
                    // Verifica se o primeiro objeto selecionado ainda está ativo
                    if (currentEventSystem.firstSelectedGameObject.activeInHierarchy)
                    {
                        currentlySelected = currentEventSystem.firstSelectedGameObject;
                        var selectable = currentlySelected.GetComponent<Selectable>();
                        if (selectable != null && selectable.interactable)
                        {
                            selectable.Select();
                        }
                    }
                }
            }
        }
    }
}
