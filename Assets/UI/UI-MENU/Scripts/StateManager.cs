using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


public class StateManager : Singleton<StateManager>
{
    [Tooltip("You need to add all of your states in here.")]
    [Header("List of States")]
    [SerializeField]
    public List<GameObject> States = new List<GameObject>();


    [Tooltip("Assign starting state in here.")]
    public CanvasType FirstCanvas;

    [HideInInspector]
    public Animator CanvasAnimator;

    
    List<StateController> canvasControllerList;

    [HideInInspector]
    public StateController ActiveCanvas;
    [HideInInspector]
    public StateController PreviousCanvas;

    private InspectManager inspectManager;

    


    protected override void Awake()
    {
        
        foreach (GameObject states in States) 
        { 
            if(states != null)
                states.SetActive(true);
        }
        
        base.Awake();

    inspectManager = FindAnyObjectByType<InspectManager>();
        
        canvasControllerList = GetComponentsInChildren<StateController>().ToList();
        canvasControllerList.ForEach(x => x.gameObject.SetActive(false));
        StartCoroutine(PlayNextCanvasAnimation(FirstCanvas));

    }

    private void OnEnable()
    {
        StartCoroutine(PlayNextCanvasAnimation(FirstCanvas));
    }
    
    public void GoToNextCanvas(CanvasType _type)
    {
        if (ActiveCanvas != null)
        {
            ActiveCanvas.gameObject.SetActive(false);
        }

    inspectManager?.DeactiveInspector();

        

        StateController NextCanvas = canvasControllerList.Find(x => x.canvasType == _type);
        if (NextCanvas != null)
        {

            PreviousCanvas = ActiveCanvas;
            NextCanvas.gameObject.SetActive(true);
            ActiveCanvas = NextCanvas;
            if(NextCanvas.GetComponent<StateController>().StartSelectable != null)
                NextCanvas.GetComponent<StateController>().StartSelectable.Select();
        }
        else { Debug.LogWarning("The next canvas was not found!"); }


    }

    public void GoToPreviousCanvas()
    {
        if (ActiveCanvas != null)
        {
            ActiveCanvas.gameObject.SetActive(false);
        }

        inspectManager.DeactiveInspector();

        StateController NextCanvas = canvasControllerList.Find(x => x.canvasType == ActiveCanvas.previousCanvas);

        Debug.Log(NextCanvas);

        if (ActiveCanvas.canvasType.canGoPreviousCanvas == true)
        {
            PreviousCanvas = ActiveCanvas;
            NextCanvas.gameObject.SetActive(true);
            ActiveCanvas = NextCanvas;
            NextCanvas.GetComponent<StateController>().StartSelectable.Select();

            Debug.Log("Can go previous canvas.");

        }
        else
        {
            Debug.Log("Can't go previous canvas.");
        }
        //Debug.Log("Go Back Performed");
    }

    

    public IEnumerator PlayNextCanvasAnimation(CanvasType _type)
    {
        
        CanvasAnimator.Play("out_canvas");
        yield return new WaitForSeconds(0.1f);
        GoToNextCanvas(_type);

        CanvasAnimator.Play("in_canvas");
    }

    public IEnumerator PlayPreviousCanvasAnimation()
    {

        CanvasAnimator.Play("out_canvas");
        yield return new WaitForSeconds(0.1f);
        GoToPreviousCanvas();
        CanvasAnimator.Play("in_canvas");

    }

    public void LeaveGame()
    {
        Application.Quit();
        Debug.Log("When you build, your game will close when submit this button.");
    }

    public void DesativarStates()
    {
        foreach (GameObject states in States) 
        { 
            if(states != null)
                states.SetActive(false);
        }
    }




}
