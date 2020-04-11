using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class syfPanelPara : MonoBehaviour
{
    public Dictionary<string, GameObject> paraDict;

    // Start is called before the first frame update
    public void SetParaDict()
    {
        paraDict = new Dictionary<string, GameObject>();
        foreach (Dropdown c in this.GetComponentsInChildren<Dropdown>())
        {
            string key = c.transform.parent.GetComponentInChildren<Text>().text;
            paraDict.Add(key.ToLower(), c.gameObject);
        }
        foreach (InputField c in this.GetComponentsInChildren<InputField>())
        {
            string key = c.transform.parent.GetComponentInChildren<Text>().text;
            paraDict.Add(key.ToLower(), c.gameObject);
        }
    }
}
