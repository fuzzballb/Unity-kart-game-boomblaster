using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Shockwave : MonoBehaviour
{
    public float MaxRadius = 10;
    public float Duration = 2;

    private float time = 0;

    public void Update()
    {
        if (time > Duration)
            time = 0;
        time += Time.deltaTime;

        transform.localScale = Vector3.one * (time / Duration) * MaxRadius;
        renderer.material.SetFloat("_Transparency", 1 - Mathf.Clamp01(time / Duration));
    }
}