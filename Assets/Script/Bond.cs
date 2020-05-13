using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bond : MonoBehaviour
{
    public Atom fromAtom = null;
    public Atom pointToAtom = null;

    public Bond originBond = null;
    public ExtendStructure extendParent = null;

    public void Init(Atom origin, Atom pointTo, bool pointToOtherCell = false)
    {
        fromAtom = origin;
        pointToAtom = pointTo;
        originBond = this;
        extendParent = null;
    }

}
