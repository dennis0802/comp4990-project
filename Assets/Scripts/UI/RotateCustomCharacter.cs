using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateCustomCharacter : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate the pickup to make it look more interesting
        transform.Rotate(new Vector3(0, 50, 0) * Time.deltaTime);
    }
}
