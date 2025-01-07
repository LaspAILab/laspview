using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BondScript : MonoBehaviour
{
    public AtomScript fromAtom = null;
    public AtomScript pointToAtom = null;

    public BondScript originBond = null;
    public ExtendStructure extendParent = null;

    public void Init(AtomScript origin, AtomScript pointTo, bool pointToOtherCell = false)
    {
        fromAtom = origin;
        pointToAtom = pointTo;
        originBond = this;
        extendParent = null;
    }

}
