using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    bool isOpen = false;
    bool needOpen = false;
    float setTime, closedX;
    // Start is called before the first frame update
    void Start()
    {
        closedX = transform.localPosition.x;
    }

    // Update is called once per frame
    void Update()
    {
        if( isOpen != needOpen )
        {
            var ct = Time.realtimeSinceStartup;
            if(setTime + 1.0f <= ct)
            {
                var x = ct - setTime - 1.0f;
                if(x >= 1.0f)
                {
                    x = 1.0f;
                    isOpen = needOpen;
                }
                transform.localPosition = new Vector3(closedX + (needOpen?x:(1.0f - x)), 
                    transform.localPosition.y, transform.localPosition.z);
            }
        }
    }

	private void OnTriggerEnter(Collider other)
	{
        if(other.tag != "Player") return;
		Debug.LogFormat("<color=yellow>Collsion Enter {0}</color>", other.name);
        needOpen = true;
        setTime = Time.realtimeSinceStartup;
	}

	private void OnTriggerExit(Collider other)
	{
        if(other.tag != "Player") return;
		Debug.LogFormat("<color=yellow>Collsion Exit {0}</color>", other.name);
        needOpen = false;
        setTime = Time.realtimeSinceStartup;
	}
}
