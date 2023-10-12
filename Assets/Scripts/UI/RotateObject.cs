using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI{
    public class RotateObject : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            // Rotate the object to make it look more interesting
            transform.Rotate(new Vector3(0, 50, 0) * Time.deltaTime);
        }
    }
}

