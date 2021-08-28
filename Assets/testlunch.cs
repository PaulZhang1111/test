using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testlunch : MonoBehaviour
{
    private string[] paramKeys = {"MRParam", "USER_DATA"};
    public Cameo.AppLauncher appLauncher;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(appLauncher.CheckCalledParamProcess(paramKeys));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
