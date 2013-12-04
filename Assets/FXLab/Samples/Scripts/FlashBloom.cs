using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class FlashBloom : MonoBehaviour
{
    public float Duration = 3.0f;
    public float extraBrightness = 2;
    public float extraBias = -0.5f;
    public float extraBlur = 0.1f;

    public Material Bloom;

    private bool isFlashing = false;
    private float flashTime;
    private float startBrightness;
    private float startBias;
    private float startBlur;

    void Awake()
    {
        startBrightness = Bloom.GetFloat("_Brightness");
        startBias = Bloom.GetFloat("_Bias");
        startBlur = Bloom.GetFloat("_BlurRange");
    }

    void Update()
    {
        if (!isFlashing)
            return;

        flashTime += Time.deltaTime;
        if (flashTime >= Duration)
            isFlashing = false;

        if (isFlashing)
        {
            var t = Mathf.Clamp01(flashTime / Duration);
            t = 1 - Mathf.Abs(t * 2 - 1);
            Bloom.SetFloat("_Brightness", startBrightness + Mathf.Lerp(0, extraBrightness, t));
            Bloom.SetFloat("_Bias", startBias + Mathf.Lerp(0, extraBias, t));
            Bloom.SetFloat("_BlurRange", startBlur + Mathf.Lerp(0, extraBlur, t));
        }
        else
        {
            Bloom.SetFloat("_Brightness", startBrightness);
            Bloom.SetFloat("_Bias", startBias);
            Bloom.SetFloat("_BlurRange", startBlur);
        }
    }

    void OnGUI()
    {
        GUI.enabled = !isFlashing;

        if (GUILayout.Button("Flash"))
        {
            isFlashing = true;
            flashTime = 0;
        }
    }
}