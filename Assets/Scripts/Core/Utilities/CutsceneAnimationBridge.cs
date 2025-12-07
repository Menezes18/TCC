using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class CutsceneAnimationBridge : MonoBehaviour
{

    public UnityEvent cutsceneEvent;

    public float delayBeforeCutscene = 1f;
    public string createRoomPopupTitle = "Criando Partida";
    public string joinRoomPopupTitle = "Entrando na Partida";
    public void StartCutscene()
    {
        if (ManagerCutscene.Instance.id == CutsceneID.CreateRoom)
        {
            PopupManager.instance.Popup_Show(createRoomPopupTitle, false, true);
        }
        else if (ManagerCutscene.Instance.id == CutsceneID.JoinRoom)
        {
            PopupManager.instance.Popup_Show(joinRoomPopupTitle, false, true);
        }
        
        cutsceneEvent?.Invoke();
        StartCoroutine(CallPopupAndCutsceneWithDelay());
    }

    private IEnumerator CallPopupAndCutsceneWithDelay()
    {
        yield return new WaitForSeconds(delayBeforeCutscene);
        CallCutscene();
    }

    public void CallCutscene()
    {
        ManagerCutscene.CallCutsceneByID(CutsceneID.CreateRoom);
    }
}

