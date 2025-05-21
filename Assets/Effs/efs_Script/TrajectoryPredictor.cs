using UnityEngine;
using Mirror;
using UnityEngine.ProBuilder.Shapes;

public class TrajectoryPredictor  : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Database db;
    [SerializeField] private PlayerScript playerScript;

    [Header("Configurações da Trajetória")]
    [SerializeField] private int linePoints = 50;
    [SerializeField] private float timeBetweenPoints = 0.05f;
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private Color startColor = Color.blue;
    [SerializeField] private Color endColor = new Color(0, 0.5f, 1f, 0.4f);
    [SerializeField] private float maxDistance = 30f;

    [Header("Mask")]
    private LayerMask ProjectCollisionMask;
    private bool isTrajectoryVisible = false;

    //[Header("HitPoint")]
    //public GameObject hitObj;

    private void Awake()
    {
        // Inicializa o Line Renderer se ainda não existir
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            //SetupLineRenderer();
        }

        // Pega o componente PlayerScript se não for atribuído
        if (playerScript == null)
        {
            playerScript = GetComponent<PlayerScript>();

            // Se ainda for nulo, tenta encontrar no parent
            if (playerScript == null)
            {
                playerScript = GetComponentInParent<PlayerScript>();
            }
        }

        //int projectLayer = gameObject.layer;
        //for (int i = 0; i < 32; i++)
        //{
        //    if (!Physics.GetIgnoreLayerCollision(projectLayer, i))
        //    {
        //        ProjectCollisionMask |= 1 << i;
        //    }
        //}
    }

    void Start()
    {
        HideTrajectory();
        cameraTransform = Camera.main.transform;
    }


    private void Update()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
        // Verificar se o PlayerScript está disponível
        if (playerScript == null) return;

        // Verificar o estado do jogador para mostrar/esconder a trajetória
        if (playerScript.Status == PlayerStatus.ThrowPrepare && !isTrajectoryVisible)
        {
            ShowTrajectory();
            isTrajectoryVisible = true;
        }
        else if (playerScript.Status != PlayerStatus.ThrowPrepare && isTrajectoryVisible)
        {
            HideTrajectory();
            isTrajectoryVisible = false;
        }

        // Atualizar a trajetória enquanto estiver visível
        if (isTrajectoryVisible)
        {
            DrawProjection();
        }
    }
    
    private void DrawProjection()
    {
        lineRenderer.enabled = true;
        lineRenderer.positionCount = Mathf.CeilToInt(linePoints / timeBetweenPoints) + 1;

        // Posição inicial do projétil
        Vector3 startPosition = playerScript.origin.transform.position;

        // Calcula a velocidade inicial baseada na direção da câmera e velocidade do projétil
        Vector3 direction = cameraTransform.forward;
        Vector3 biasedDir = (direction + Vector3.up * db.verticalBias).normalized;
        Vector3 startVelocity = biasedDir * db.projectileSpeed;

        int i = 0;
        lineRenderer.SetPosition(i, startPosition);

        for (float time = 0; time < linePoints; time += timeBetweenPoints)
        {
            i++;

            // Calcula a posição usando a equação de movimento parabólico
            Vector3 point = startPosition + time * startVelocity;
            point.y = startPosition.y + startVelocity.y * time + (Physics.gravity.y * db.projectileGravityScale / 2f * time * time);

            lineRenderer.SetPosition(i, point);

            // Verifica colisão para terminar a linha no ponto de impacto
            if (i > 0)
            {
                Vector3 prevPos = lineRenderer.GetPosition(i - 1);
                RaycastHit hit;
                if (Physics.Raycast(prevPos, (point - prevPos).normalized, out hit, Vector3.Distance(prevPos, point), db.projectileMask))
                {
                    // Termina a linha no ponto de impacto
                    lineRenderer.SetPosition(i, hit.point);
                    // Desativa os pontos restantes colocando-os na mesma posição
                    for (int j = i + 1; j < lineRenderer.positionCount; j++)
                    {
                        lineRenderer.SetPosition(j, hit.point);
                    }
                    break;
                }
            }

            // Verifica se a trajetória atingiu a distância máxima
            if (Vector3.Distance(startPosition, point) > maxDistance)
            {
                lineRenderer.positionCount = i + 1;
                break;
            }
            Vector3 lastPosition = lineRenderer.GetPosition(i - 1);

            
            //if (Physics.Raycast(lastPosition,
            //    (point - lastPosition).normalized,
            //    out RaycastHit hit2,
            //    (point - lastPosition).magnitude,
            //    ProjectCollisionMask))
            //{
            //    lineRenderer.SetPosition(i, hit2.point);
            //    lineRenderer.positionCount = i + 1;
            //    return;        
            //}

        }
        //if (!hitObj.activeInHierarchy)
        //{
        //    hitObj.transform.position = lineRenderer.GetPosition(i-1);
        //    hitObj.SetActive(true);
        //}
            
    }

    public void ShowTrajectory()
    {
        lineRenderer.enabled = true;
    }

    public void HideTrajectory()
    {
        lineRenderer.enabled = false;
    }
}