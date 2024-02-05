using System.Collections;
using System.Collections.Generic;
using ExternPropertyAttributes;
using UnityEditor.Animations;
using UnityEngine;

public class RouletteController : MonoBehaviour
{
    [Header("Wheel")]
    [SerializeField]float spinDurationMin;
    [SerializeField]float spinDurationMax;
    [SerializeField]float rotationSpeed;
    public Transform _rouletteNumbers;

    [Header("Ball")]
    [SerializeField]GameObject rouletteBall;
    [SerializeField]GameObject rouletteBallHome;
    [SerializeField] float ballSpeed = 30.0f;
    [SerializeField] float ballRadius = 180.0f;

    #pragma warning disable
    private bool isSpinning = false;
    #pragma warning restore
    

    public Material testMat;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RouletteSpin();
        }
    }

    void RouletteSpin()
    {
        isSpinning = true;

        StartCoroutine(SpinWheel());

    }
        
    void WheelResult(Transform winner)
    {
        rouletteBall.transform.position = winner.GetChild(0).position;
        foreach(Transform child in winner)
        {
            child.GetComponent<Renderer>().material = testMat;
        }
    }
    IEnumerator SpinWheel()
    {
        Transform winner = _rouletteNumbers.GetChild(Random.Range(0, 32));

        //for Wheel
        float spinDuration = Random.Range(spinDurationMin, spinDurationMax);
        float rotSpeed = rotationSpeed / spinDuration;
        float elapsedTime = 0.0f;
        
        //for ball
        
        float ballAngle = 0.0f;
        rouletteBall.transform.position = rouletteBallHome.transform.position;
        rouletteBall.SetActive(true);


        while (elapsedTime < spinDuration)
        {
            // Rotate the wheel
            transform.Rotate(Vector3.up, rotSpeed * Time.deltaTime);

            //ball
            ballAngle += ballSpeed * Time.deltaTime;
            float posX = Mathf.Cos(ballAngle * Mathf.Deg2Rad) * ballRadius;
            float posZ = Mathf.Sin(ballAngle * Mathf.Deg2Rad) * ballRadius;
            rouletteBall.transform.position = new Vector3(posX, rouletteBall.transform.position.y, posZ);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        isSpinning = false;
        
        WheelResult(winner);
    }
}
