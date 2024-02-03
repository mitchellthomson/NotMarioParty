using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouletteSlotManager : MonoBehaviour
{
    public GameObject lane;
    
    void Start()
    {
        print(transform.position);
        print(transform.localPosition);

        transform.position = lane.transform.position;

        print(transform.position);
        print(transform.localPosition);
    }

}
