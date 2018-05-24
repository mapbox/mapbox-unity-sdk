using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class InstructionUXCanvas : MonoBehaviour
{



    public Text instructionText;
    public Button closeButton;
    public static InstructionUXCanvas Instance = null;
    // Use this for initialization
    private void Awake()
    {
        Instance = this;
        closeButton.gameObject.SetActive(false);


    }


    public void SetInstruction(InstructionObject I)
    {
        instructionText.text = I.InstructionText;
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(I.canvasEvent.Invoke);


    }

    public void SetButtonActive()
    {
        closeButton.gameObject.SetActive(true);


    }

    // Update is called once per frame
    void Update()
    {

    }
}

[System.Serializable]
public class InstructionObject
{
    public string InstructionText;
    public UnityEvent canvasEvent;
}


