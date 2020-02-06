using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProjectDescription : MonoBehaviour
{
    public float outY = 150f;
    public float inY = 90f;

    Image panel;
    TextMeshProUGUI text;

    float curTime;
    Vector3 outPos;
    Vector3 inPos;

    Color panelColor;
    Color32 textColor;
    void Awake()
    {
        panel = GetComponent<Image>();
        panelColor = panel.color;
        text = GetComponentInChildren<TextMeshProUGUI>();
        textColor = text.color;

        Color tempColor = panelColor;
        tempColor.a = 0f;
        panel.color = tempColor;

        Color tempColor32 = textColor;
        tempColor32.a = 0;
        text.color = tempColor32;
    }

    void Start()
    {
        outPos = new Vector3(0, outY, 0);
        inPos = new Vector3(0, inY, 0);
    }

    public IEnumerator SlideIn(float duration)
    {
        Color tempColor = panel.color;
        Color32 tempTextColor = text.color;
        while (curTime < 1.0f)
        {
            this.transform.localPosition = Vector3.Lerp(outPos, inPos, curTime);

            tempColor.a = curTime * curTime * panelColor.a;
            panel.color = tempColor;

            tempTextColor.a = (byte)(curTime * curTime * textColor.a);
            text.color = tempTextColor;

            curTime += (Time.deltaTime / duration);
            yield return null;
        }
        curTime = 1.0f;

        this.transform.localPosition = inPos;

        panel.color = panelColor;

        text.color = textColor;
    }

    public IEnumerator SlideOut(float duration)
    {
        Color tempColor = panel.color;
        Color32 tempTextColor = text.color;
        while (curTime > 0.0f)
        {
            this.transform.localPosition = Vector3.Lerp(outPos, inPos, curTime);

            tempColor.a = curTime * curTime * panelColor.a;
            panel.color = tempColor;

            tempTextColor.a = (byte)(curTime * curTime * textColor.a);
            text.color = tempTextColor;

            curTime -= (Time.deltaTime / duration);
            yield return null;
        }
        curTime = 0.0f;
        this.transform.localPosition = outPos;

        tempColor.a = 0.0f;
        panel.color = tempColor;

        tempTextColor.a = 0;
        text.color = tempTextColor;
    }
}
