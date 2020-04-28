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
        foreach (Dropdown c in this.gameObject.GetComponentsInChildren<Dropdown>())
        {
            string key = c.transform.parent.GetComponentInChildren<Text>().text;
            paraDict.Add(key.ToLower().Trim(), c.gameObject);
        }
        foreach (InputField c in this.gameObject.GetComponentsInChildren<InputField>())
        {
            string key = c.transform.parent.GetComponentInChildren<Text>().text;
            paraDict.Add(key.ToLower().Trim(), c.gameObject);
        }
        foreach (Button c in this.GetComponentsInChildren<Button>())
        {
            if (c.GetComponentInChildren<Text>())
            {
                string key = c.GetComponentInChildren<Text>().text;
                paraDict.Add(key.ToLower().Trim(), c.gameObject);
            }
        }        
        foreach(Slider c in this.GetComponentsInChildren<Slider>())
        {

            string key = c.transform.parent.GetComponentInChildren<Text>().text;
            paraDict.Add(key.ToLower().Trim(), c.gameObject);
        }
    }
}
