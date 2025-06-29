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
                currentlySelected.GetComponent<Selectable>().Select();
            }
            else
            {
                // If there is none, select the firstSelectedGameObject
                // (which can be setup inthe EventSystem component).
                currentlySelected = currentEventSystem.firstSelectedGameObject;
                currentlySelected.GetComponent<Selectable>().Select();
            }
        }
    }
}
