using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Abcd : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Vector3 v = new Vector3(1, 2, 3);
        v.Normalize();  // v의 길이를 1로 만든다.
    }

    // Update is called once per frame
    void Update()
    {
        Transform tr = gameObject.transform;
    }
}
