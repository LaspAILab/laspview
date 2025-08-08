using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class PeriodicTableControl : VisualElement
{
    const string ELEMENT_BUTTON_CLASS = "-periodic-table-button";
    
    public delegate void ElementClickedEventHandler(string elementSymbol);
    
    public event ElementClickedEventHandler ElementClickedEvent;

    private static VisualElement NewRow => new VisualElement
    {
        style =
        {
            flexDirection = FlexDirection.Row
        }
    };
    
    private static readonly string[] ElementSymbols = {
        "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne",
        "Na", "Mg", "Al", "Si", "P", "S", "Cl", "Ar", "K", "Ca",
        "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
        "Ga", "Ge", "As", "Se", "Br", "Kr", "Rb", "Sr", "Y", "Zr",
        "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd", "In", "Sn",
        "Sb", "Te", "I", "Xe", "Cs", "Ba", "La", "Ce", "Pr", "Nd",
        "Pm", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb",
        "Lu", "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg",
        "Tl", "Pb", "Bi", "Po", "At", "Rn", "Fr", "Ra", "Ac", "Th",
        "Pa", "U", "Np", "Pu", "Am", "Cm", "Bk", "Cf", "Es", "Fm",
        "Md", "No", "Lr", "Rf", "Db", "Sg", "Bh", "Hs", "Mt", "Ds",
        "Rg", "Cn", "Nh", "Fl", "Mc", "Lv", "Ts", "Og"
    };
    private static readonly int[] GapPositions = {1, 4, 12, 56, 88};
    private static readonly int[] LineBreakPositions = { 2, 10, 18, 36, 54, 86 };
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public PeriodicTableControl()
    {
        VisualElement currentRow = NewRow;
        
        for (int i = 0; i < ElementSymbols.Length; i++)
        {
            // Special handle for F-block
            if ((i + 1 >= 57 && i + 1 <= 71) || (i + 1 >= 89 && i + 1 <= 103))
            {
                continue;
            }
            
            Button elementButton = new();
            elementButton.RegisterCallback<ClickEvent>(OnElementClicked);
            elementButton.AddToClassList(ELEMENT_BUTTON_CLASS);
            elementButton.text = ElementSymbols[i];
            currentRow.Add(elementButton);
            
            if (GapPositions.Contains(i + 1))
            {
                // Add whitespace
                currentRow.Add(new VisualElement
                {
                    style =
                    {
                        flexGrow = 1
                    }
                });
            }
            
            if (LineBreakPositions.Contains(i + 1))
            {
                Add(currentRow);
                currentRow = NewRow;
            }
        }
        
        Add(currentRow);
        Add(new VisualElement
        {
            style =
            {
                height = 40
            }
        });

        currentRow = NewRow;
        currentRow.style.justifyContent = Justify.Center;
        for (int i = 56; i <= 70; i++)
        {
            Button elementButton = new();
            elementButton.RegisterCallback<ClickEvent>(OnElementClicked);
            elementButton.AddToClassList(ELEMENT_BUTTON_CLASS);
            elementButton.text = ElementSymbols[i];
            currentRow.Add(elementButton);
        }
        Add(currentRow);
        
        currentRow = NewRow;
        currentRow.style.justifyContent = Justify.Center;
        for (int i = 88; i <= 102; i++)
        {
            Button elementButton = new();
            elementButton.RegisterCallback<ClickEvent>(OnElementClicked);
            elementButton.AddToClassList(ELEMENT_BUTTON_CLASS);
            elementButton.text = ElementSymbols[i];
            currentRow.Add(elementButton);
        }
        Add(currentRow);
    }

    public void UnregisterAllListeners()
    {
        // Clear all handlers for the event
        ElementClickedEvent = null;
    }

    void OnElementClicked(ClickEvent evt)
    {
        if (evt.target is Button button)
        {
            ElementClickedEvent?.Invoke(button.text);
        }
    }
}