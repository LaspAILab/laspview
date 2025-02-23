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

    public void SetValue(Vector3 value)
    {
        inputFields[0].text = value.x.ToString("0.0##");
        inputFields[1].text = value.y.ToString("0.0##");
        inputFields[2].text = value.z.ToString("0.0##");
    }
    
    public void SetValue(float x, float y, float z)
    {
        inputFields[0].text = x.ToString("0.0##");
        inputFields[1].text = y.ToString("0.0##");
        inputFields[2].text = z.ToString("0.0##");
    }
}
