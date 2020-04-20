using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class M_LaspInWidgetSrc : MonoBehaviour
{
    static public M_LaspInWidgetSrc S;
    public InputField mainText;
    public Button closeBtn;
    public Button saveBtn;

    public string returnToString;
    public Button returnToButton;


    void Awake()
    {
        S = this;
        mainText = this.gameObject.GetComponentInChildren<InputField>();
        saveBtn = transform.GetChild(0).GetChild(1).GetComponent<Button>();
        closeBtn = transform.GetChild(0).GetChild(2).GetComponent<Button>();

        //Set lasp.in Editor Widget
        this.transform.SetParent(GameObject.Find("Canvas").transform);
        this.transform.position = new Vector3(Screen.width / 2, Screen.height / 2);
        this.gameObject.SetActive(false);
    }


    public void LaspInSave()
    {
        returnToString = mainText.text;
    }

    public void LaspInQuit()
    {

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
