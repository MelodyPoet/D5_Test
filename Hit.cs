using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Mathf.Abs(collision.transform.position.y - transform.parent.position.y) > 0.3) return;
        collision.gameObject.SendMessage("OnHit",collision.transform.position.x>transform.parent.position.x?1:-1);
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
      
    }
}
