using UnityEngine;
using System.Collections;

public class Caca : MonoBehaviour
{
    public float shapekeyRate = 0.3f;
    public float shapekeyRefreshRate = 0.01f;
    public float shapekeyDelay = 1.25f;
    public SkinnedMeshRenderer shapekeyObject;
    void Start()
    {
        if(shapekeyObject == null)shapekeyObject = GetComponent<SkinnedMeshRenderer>();
        StartCoroutine("ShapeChange");   
    }
    IEnumerator ShapeChange(){
        yield return new WaitForSeconds(shapekeyDelay);

        float t = 0.1f;
        while (t > 0)
        {
            t -= shapekeyRate;
            shapekeyObject.SetBlendShapeWeight(0,t);
            yield return new WaitForSeconds(shapekeyRefreshRate);
        }
        yield return new WaitForSeconds(5f);
        while (t < 100)
        {
            t += shapekeyRate;
            shapekeyObject.SetBlendShapeWeight(0,t);
            yield return new WaitForSeconds(shapekeyRefreshRate);
        }
        Debug.Log("shapekeyObject");
        transform.position = Vector3.zero;
        gameObject.SetActive(false);
    }
    
}
