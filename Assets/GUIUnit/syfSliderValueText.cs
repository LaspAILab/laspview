using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class syfSliderValueText : MonoBehaviour
{
    
    // Start is called before the first frame update
    void Start()
    {
    }

    public void ChangeText(string format = "f2")
    {
        Text t = this.gameObject.transform.GetChild(1).GetComponent<Text>();
        Slider s = this.gameObject.transform.GetChild(2).GetComponent<Slider>();
        t.text = string.Format("{0:"+format+"}", s.value); 
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
