using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class syfPanelPara : MonoBehaviour
{
    public Dictionary<string, Component> paraDict;

    // Start is called before the first frame update
    public void SetParaDict()
    {
        paraDict = new Dictionary<string, Component>();
        foreach (Dropdown c in this.GetComponentsInChildren<Dropdown>())
        {
            string key = c.transform.parent.GetComponentInChildren<Text>().text;
            Text value = c.captionText;

            //print((key, value.text));
            paraDict.Add(key, value);
        }
    }
}
