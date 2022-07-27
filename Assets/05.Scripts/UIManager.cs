using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    private void Awake()
    {
        instance = this;
    }

    float _Sec;
    int _Min;

    public GameObject IONIQ;
    public Slider speedometer;

    public TextMeshProUGUI time;

    public Image SD1;
    public Image SD2;
    public Image SD3;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Timer();
        Speed();
        SafeDistance0();
        SafeDistance2();
        SafeDistance4();
        SafeDistance8();
    }

    public void SafeDistance8()
    {
        SD1.enabled = false;
        SD2.enabled = false;
        SD3.enabled = true;
    }

    public void SafeDistance4()
    {
        SD1.enabled = false;
        SD2.enabled = true;
        SD3.enabled = false;
    }

    public void SafeDistance2()
    {
        SD1.enabled = true;
        SD2.enabled = false;
        SD3.enabled = false;
    }
    public void SafeDistance0()
    {
        SD1.enabled = true;
        SD2.enabled = false;
        SD3.enabled = false;
        StartCoroutine(Warning());
    }

    IEnumerator Warning()
    {
        int count = 0;
        while (count < 3)
        {
            SD1.enabled = true;
            yield return new WaitForSeconds(0.5f);
            SD1.enabled = false;
            yield return new WaitForSeconds(0.5f);
            count++;
            break;
        }
    }

    private void Speed()
    {
        /*currentPosition = transform.position;
        var dis = (currentPosition - oldPosition);
        var distance = Math.Sqrt(Math.Pow(dis.x, 2) + Math.Pow(dis.y, 2) + Math.Pow(dis.z, 2));
        velocity = distance / Time.deltaTime;
        speed.text = velocity + "km/h";
        oldPosition = currentPosition;*/

        speedometer.value = NNFlat.instance.rb.velocity.sqrMagnitude;

    }

    private void Timer()
    {
        _Sec += Time.deltaTime;

        time.text = string.Format("{0:D2}:{1:D2}", _Min, (int)_Sec);

        if ((int)_Sec > 59)
        {
            _Sec = 0;
            _Min++;
        }
    }

}
