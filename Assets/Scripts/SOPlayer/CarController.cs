using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private CarData carData; 

    private void Update()
    {
        
        transform.Translate(Vector3.forward * carData.speed * Time.deltaTime);
    }

    
    public void SetCarData(CarData data)
    {
        carData = data;
    }
}