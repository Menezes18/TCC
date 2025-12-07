using UnityEngine;
using UnityEngine.Events;

public enum CutsceneID
{
    None,
    CreateRoom,
    JoinRoom
}

public class ManagerCutscene : MonoBehaviour
{
    private static ManagerCutscene _instance;
    
    public static ManagerCutscene Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ManagerCutscene>();
                
                if (_instance == null)
                {
                    GameObject go = new GameObject("ManagerCutscene");
                    _instance = go.AddComponent<ManagerCutscene>();
                }
            }
            return _instance;
        }
    }

    [Header("Cutscene Settings")]
    public CutsceneID id;
    public UnityEvent callCreateRoomEvent;
    public UnityEvent callJoinRoomEvent;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }


    public void setCutsceneID(CutsceneID id)
    {
        this.id = id;
    }


    public void callCutscene()
    {
        if (id == CutsceneID.CreateRoom)
        {
            callCreateRoomEvent?.Invoke();
        }
        else if (id == CutsceneID.JoinRoom)
        {
            callJoinRoomEvent?.Invoke();
        }
    }

    public void setCutsceneIDByInt(int id)
    {
        switch (id)
        {
            case 0:
                this.id = CutsceneID.None;
                break;
            case 1:
                this.id = CutsceneID.CreateRoom;
                break;
            case 2:
                this.id = CutsceneID.JoinRoom;
                break;
        }
    }

    public static void CallCutsceneByID(CutsceneID cutsceneID)
    {
        if (Instance != null)
        {
            Instance.setCutsceneID(cutsceneID);
            Instance.callCutscene();
        }
    }


    public static void CallCurrentCutscene()
    {
        if (Instance != null)
        {
            Instance.callCutscene();
        }
    }
}
