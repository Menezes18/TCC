using Mirror;
using UnityEngine;
using UnityEngine.Events;


public class CinematicCameraController : NetworkBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private GameObject cinematicCamera;
    [SerializeField] private bool startActive = false;
    [SerializeField] private bool disablePlayerCameraOnActivate = true;

    [Header("Timing")]
    [SerializeField] private float activationDelay = 0f;
    [SerializeField] private float deactivationDelay = 0f;

    [Header("Events VFX SLA")]
    public UnityEvent onCameraActivated;
    public UnityEvent onCameraDeactivated;

    [Header("Player Control")]
    [SerializeField] private bool freezePlayersOnActivate = true;
    [SerializeField] private bool unfreezePlayersOnDeactivate = true;

    [SyncVar(hook = nameof(OnCameraStateChanged))]
    private bool isCameraActive = false;

    private Coroutine _activationCoroutine;
    private Coroutine _deactivationCoroutine;

    private void Start()
    {
        if (startActive && isServer)
        {
            ServerActivateCamera();
        }
        else if (!startActive)
        {

            if (cinematicCamera != null)
                cinematicCamera.SetActive(false);
        }
    }

    #region Server Methods

    /// <summary>
    /// [Server] Ativa a câmera imediatamente (ignora delay configurado)
    /// </summary>
    [Server]
    public void ServerActivateCameraImmediate()
    {
        ServerActivateCameraInternal(0f);
    }

    /// <summary>
    /// [Server] Ativa a câmera usando o delay configurado no Inspector
    /// </summary>
    [Server]
    public void ServerActivateCamera()
    {
        ServerActivateCameraInternal(activationDelay);
    }

    /// <summary>
    /// [Server] Ativa a câmera com um delay customizado
    /// </summary>
    [Server]
    public void ServerActivateCamera(float customDelay)
    {
        ServerActivateCameraInternal(customDelay);
    }

    [Server]
    private void ServerActivateCameraInternal(float delay)
    {
        if (isCameraActive)
        {
            Debug.LogWarning("[CinematicCamera] Câmera já está ativa");
            return;
        }

        // Cancela qualquer ativação pendente
        if (_activationCoroutine != null)
        {
            StopCoroutine(_activationCoroutine);
            _activationCoroutine = null;
        }

        if (delay > 0f)
        {
            Debug.Log($"⏰ [CinematicCamera] Ativando câmera em {delay} segundos");
            _activationCoroutine = StartCoroutine(ActivateCameraWithDelayRoutine(delay));
        }
        else
        {
            InternalServerActivateCamera();
        }
    }

    [Server]
    private System.Collections.IEnumerator ActivateCameraWithDelayRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        InternalServerActivateCamera();
        _activationCoroutine = null;
    }

    [Server]
    private void InternalServerActivateCamera()
    {
        Debug.Log("🎬 [CinematicCamera] Ativando câmera cinematográfica");
        isCameraActive = true;

        if (freezePlayersOnActivate && PlayerList.singleton != null)
        {
            PlayerList.singleton.SetAllPlayersFrozen(true);
        }

        RpcActivateCamera();
    }


    [Server]
    public void ServerDeactivateCameraImmediate()
    {
        ServerDeactivateCameraInternal(0f);
    }


    [Server]
    public void ServerDeactivateCamera()
    {
        ServerDeactivateCameraInternal(deactivationDelay);
    }


    [Server]
    public void ServerDeactivateCamera(float customDelay)
    {
        ServerDeactivateCameraInternal(customDelay);
    }

    [Server]
    private void ServerDeactivateCameraInternal(float delay)
    {
        if (!isCameraActive)
        {
            Debug.LogWarning("[CinematicCamera] Câmera já está desativada");
            return;
        }

        if (_deactivationCoroutine != null)
        {
            StopCoroutine(_deactivationCoroutine);
            _deactivationCoroutine = null;
        }

        if (delay > 0f)
        {
            Debug.Log($"⏰ [CinematicCamera] Desativando câmera em {delay} segundos");
            _deactivationCoroutine = StartCoroutine(DeactivateCameraWithDelayRoutine(delay));
        }
        else
        {
            InternalServerDeactivateCamera();
        }
    }

    [Server]
    private System.Collections.IEnumerator DeactivateCameraWithDelayRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        InternalServerDeactivateCamera();
        _deactivationCoroutine = null;
    }

    [Server]
    private void InternalServerDeactivateCamera()
    {
        Debug.Log("🎬 [CinematicCamera] Desativando câmera cinematográfica");
        isCameraActive = false;

        if (unfreezePlayersOnDeactivate && PlayerList.singleton != null)
        {
            PlayerList.singleton.SetAllPlayersFrozen(false);
        }

        RpcDeactivateCamera();
    }


    [Server]
    public void ServerToggleCamera()
    {
        if (isCameraActive)
            ServerDeactivateCameraImmediate();
        else
            ServerActivateCameraImmediate();
    }


    [Server]
    public void ServerCancelPendingActions()
    {
        if (_activationCoroutine != null)
        {
            StopCoroutine(_activationCoroutine);
            _activationCoroutine = null;
            Debug.Log("❌ [CinematicCamera] Ativação cancelada");
        }

        if (_deactivationCoroutine != null)
        {
            StopCoroutine(_deactivationCoroutine);
            _deactivationCoroutine = null;
            Debug.Log("❌ [CinematicCamera] Desativação cancelada");
        }
    }

    #endregion

    #region Client RPC

    [ClientRpc]
    private void RpcActivateCamera()
    {
        InternalActivateCamera();
    }

    [ClientRpc]
    private void RpcDeactivateCamera()
    {
        InternalDeactivateCamera();
    }

    #endregion

    #region Internal Methods

    private void InternalActivateCamera()
    {
        if (cinematicCamera != null)
        {
            cinematicCamera.SetActive(true);
            Debug.Log("📹 [CLIENT] Câmera cinematográfica ativada");
        }



        onCameraActivated?.Invoke();
    }

    private void InternalDeactivateCamera()
    {
        if (cinematicCamera != null)
        {
            cinematicCamera.SetActive(false);
            Debug.Log("📹 [CLIENT] Câmera cinematográfica desativada");
        }



        onCameraDeactivated?.Invoke();
    }

    /*
    private void DisablePlayerCamera()
    {

        if (Camera.main != null)
        {
            Camera.main.enabled = false;
        }
    }

    private void EnablePlayerCamera()
    {

        if (Camera.main != null)
        {
            Camera.main.enabled = true;
        }
    }*/

    #endregion

    #region SyncVar Hooks

    private void OnCameraStateChanged(bool oldValue, bool newValue)
    {

        if (newValue)
        {
            InternalActivateCamera();
        }
        else
        {
            InternalDeactivateCamera();
        }
    }

    #endregion

    #region Public Getters


    public bool IsCameraActive => isCameraActive;

    #endregion
}

