using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCam : MonoBehaviour
{
    public Transform camPos;

    private void Update()
    {
        if (camPos != null)
        {
            transform.position = camPos.position;
        }
    }
}
