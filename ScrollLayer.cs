using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollLayer : MonoBehaviour
{
    private float startX;
    public float startCameraX;
    [Range(-1,1)]
    public float moveSpeed=-0.5f;
    void Start()
    {
        startX=transform.position.x;
        
    }

    // Update is called once per frame
    void Update()
    {   
        var pos=transform.position;
        pos.x=startX+(Camera.main.transform.position.x-startCameraX)*(- moveSpeed);
        transform.position=pos;
    }
}
