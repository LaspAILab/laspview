using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bond : MonoBehaviour
{
    public Atom pointToAtom;
    public bool point2OtherCell;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Init(Atom a, bool pointToOtherCell = false)
    {
        pointToAtom = a;
        point2OtherCell = pointToOtherCell;
    }

}
