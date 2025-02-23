using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class VectorInputController : MonoBehaviour
{
    private InputField[] inputFields;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inputFields = GetComponentsInChildren<InputField>();
        Assert.AreEqual(inputFields.Length, 3);
    }

    public Vector3 GetValue()
    {
        float a = float.Parse(inputFields[0].text);
        float b = float.Parse(inputFields[1].text);
        float c = float.Parse(inputFields[2].text);
        return new Vector3(a, b, c);
    }
}
