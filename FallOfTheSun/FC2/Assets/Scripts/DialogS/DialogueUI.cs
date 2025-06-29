using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
[System.Serializable]
public class DialogueLine
{
    public string speakerName;
    public Sprite speakerIcon;
    [TextArea(2, 5)]
    public string dialogueText;
}
[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue System/Dialogue")]
public class Dialogue : ScriptableObject
{
    public List<DialogueLine> lines;
}

public class DialogueUI : MonoBehaviour
{
    public GameObject panel;
    public Image speakerIcon;
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;

    private Queue<DialogueLine> linesQueue;

    public void StartDialogue(Dialogue dialogue)
    {
        panel.SetActive(true);
        linesQueue = new Queue<DialogueLine>(dialogue.lines);
        ShowNextLine();
    }

    public void ShowNextLine()
    {
        if (linesQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = linesQueue.Dequeue();

        speakerIcon.sprite = line.speakerIcon;
        speakerNameText.text = line.speakerName;
        dialogueText.text = line.dialogueText;
    }

    void EndDialogue()
    {
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel.activeSelf && Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextLine();
        }
    }
}