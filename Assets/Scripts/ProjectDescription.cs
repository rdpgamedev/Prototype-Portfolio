using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectDescription : MonoBehaviour
{
    public float outY = 150f;
    public float inY = 90f;

    CanvasGroup canvasGroup;

    float curTime;
    Vector3 outPos;
    Vector3 inPos;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        outPos = new Vector3(0, outY, 0);
        inPos = new Vector3(0, inY, 0);
    }

    public IEnumerator SlideIn(float duration)
    {
        while (curTime < 1.0f)
        {
            this.transform.localPosition = Vector3.Lerp(outPos, inPos, curTime);

            canvasGroup.alpha = curTime;

            curTime += (Time.deltaTime / duration);
            yield return null;
        }
        curTime = 1.0f;

        this.transform.localPosition = inPos;

        canvasGroup.alpha = curTime;
    }

    public IEnumerator SlideOut(float duration)
    {
        while (curTime > 0.0f)
        {
            this.transform.localPosition = Vector3.Lerp(outPos, inPos, curTime);

            canvasGroup.alpha = curTime;

            curTime -= (Time.deltaTime / duration);
            yield return null;
        }
        curTime = 0.0f;
        
        this.transform.localPosition = outPos;

        canvasGroup.alpha = curTime;
    }
}
