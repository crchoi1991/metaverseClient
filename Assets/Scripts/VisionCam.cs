using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionCam : MonoBehaviour
{
    RenderTexture vision;
    Texture2D tex;
    Rect rt;
    public Texture2D preview;
    // Start is called before the first frame update
    void Start()
    {
        var cam = gameObject.GetComponent<Camera>();
        vision = cam.targetTexture;
        tex = new Texture2D(vision.width, vision.height);
        rt = new Rect(0, 0, vision.width, vision.height);
    }

    // Update is called once per frame
    void Update()
    {
        var prev = RenderTexture.active;
        RenderTexture.active = vision;
        tex.ReadPixels(rt, 0, 0);
        tex.Apply();
        RenderTexture.active = prev;

        var pixs = tex.GetPixels32();
        var target = new Color32[vision.width*vision.height/4];
        var white = new Color32(255, 255, 255, 255);
        var black = new Color32(0, 0, 0, 255);
        for(int y = 0; y < vision.height; y+=2)
        {
            for(int x = 0; x < vision.width; x+=2)
            {
                var c = pixs[y*vision.width+x];
                if(c.r > 128 && c.g > 128 && c.b > 128)
                    target[(y/2)*(vision.width/2)+(x/2)] = c;
                else
                    target[(y/2)*(vision.width/2)+(x/2)] = black;
            }
        }
        preview.SetPixels32(target);
        preview.Apply();
    }
}
