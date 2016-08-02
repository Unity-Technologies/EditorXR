using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class RepeatButton : MonoBehaviour
{
    public Button button = null;
    public bool Repeat { get; set; }

    void Update()
    {
        if (Repeat)
            button.onClick.Invoke();
    }
}
