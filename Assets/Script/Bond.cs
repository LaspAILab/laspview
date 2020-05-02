using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bond : MonoBehaviour
{
    public Atom originAtom;
    public Atom pointToAtom;
    public Bond originBond;

    //public Bond 
    //public bool point2OtherCell;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void Init(Atom origin, Atom pointTo, bool pointToOtherCell = false)
    {
        originAtom = origin;
        pointToAtom = pointTo;
        originBond = this;
    }

}
