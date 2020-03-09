using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController1b : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.GetComponent<CapsuleCollider>().radius = Mathf.Clamp(gameObject.GetComponent<CapsuleCollider>().radius, 0.1f, 1.25f);
    }
}
