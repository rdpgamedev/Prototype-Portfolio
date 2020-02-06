using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ProjectButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public ProjectDescription description;
    public float maxScale = 2f;
    public float transitionLength = 0.5f;

    Vector3 baseScale;
    Vector3 endScale;
    float tScale = 0f;

    public void Awake()
    {
        description = GetComponentInChildren<ProjectDescription>();
    }

    public void Start()
    {
        baseScale = transform.localScale;
        endScale = baseScale * maxScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleUp());
        StartCoroutine(description.SlideIn(transitionLength));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleDown());
        StartCoroutine(description.SlideOut(transitionLength));
    }

    IEnumerator ScaleUp ()
    {
        if (tScale < 0.0f) tScale = 0.0f;
        float curTime = tScale * transitionLength;

        while (tScale < 1.0f)
        {
            transform.localScale = Vector3.Lerp(baseScale, endScale, tScale);
            curTime += Time.deltaTime;
            tScale = curTime / transitionLength;
            yield return null;
        }
        transform.localScale = baseScale * maxScale;
        yield return null;
    }

    IEnumerator ScaleDown ()
    {
        if (tScale > 1.0f) tScale = 1.0f;
        float curTime = tScale * transitionLength;

        while (tScale > 0f)
        {
            transform.localScale = Vector3.Lerp(baseScale, endScale, tScale);
            curTime -= Time.deltaTime;
            tScale = curTime / transitionLength;
            yield return null;
        }
        transform.localScale = baseScale;
        yield return null;
    }

}
