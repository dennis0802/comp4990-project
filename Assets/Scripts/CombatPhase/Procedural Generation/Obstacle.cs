using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    public bool set = false;
    public float distance = 100f;

    // Update is called once per frame
    void Update()
    {
        // Like spawnpoints, obstacles will initially be set in the air to work with the randomized terrain
        // When terrain is generated, send a raycast to the ground
        RaycastHit hit;

        // If contact with the ground found and hasn't been set yet, set the obstacle on the ground
        if(!set && Physics.Raycast(transform.position, Vector3.down, out hit, distance)) {
            Vector3 targetLocation = hit.point;
            targetLocation += new Vector3(0, transform.localScale.y / 2, 0);
            transform.position = targetLocation;
            set = true;
        }
        else if(set && transform.position.y >= 5.0f){
            Destroy(gameObject);
        }
    }
}
