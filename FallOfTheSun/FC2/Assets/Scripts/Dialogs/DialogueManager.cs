using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public GameObject dialoguePanelPrefab;

    public DialogueData[] allDialogues;

    private GameObject dialoguePanelInstance;
    private Image leftPortrait;
    private Image rightPortrait;
    private TMP_Text dialogueText;

    private int currentLine = 0;
    private DialogueData currentDialogue;

    void Start()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Brak Canvas w scenie!");
            return;
        }

        dialoguePanelInstance = Instantiate(dialoguePanelPrefab);
        dialoguePanelInstance.transform.SetParent(canvas.transform, false);

        Transform left = dialoguePanelInstance.transform.Find("LeftPortrait");
        Transform right = dialoguePanelInstance.transform.Find("RightPortrait");
        Transform text = dialoguePanelInstance.transform.Find("DialogueText");

        if (left == null || right == null || text == null)
        {
            Debug.LogError("Brak UI w panelu dialogowym.");
            return;
        }

        leftPortrait = left.GetComponent<Image>();
        rightPortrait = right.GetComponent<Image>();
        dialogueText = text.GetComponent<TMP_Text>();

        dialoguePanelInstance.SetActive(false);
    }

void Update()
{
    Debug.Log("Update działa");
    if (dialoguePanelInstance != null && dialoguePanelInstance.activeSelf)
    {
        if (Input.GetMouseButtonDown(1)) // Prawy przycisk myszy
        {
                        Debug.Log("PPM kliknięty");
            NextLine();
        }
    }
}

    // Metoda uruchamiająca dialog po nazwie
public void StartDialogueByName(string dialogueName)
{
    if (dialoguePanelInstance == null)
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Brak Canvas w scenie!");
            return;
        }

        dialoguePanelInstance = Instantiate(dialoguePanelPrefab);
        dialoguePanelInstance.transform.SetParent(canvas.transform, false);

        Transform left = dialoguePanelInstance.transform.Find("LeftPortrait");
        Transform right = dialoguePanelInstance.transform.Find("RightPortrait");
        Transform text = dialoguePanelInstance.transform.Find("DialogueText");

        if (left == null || right == null || text == null)
        {
            Debug.LogError("Brak UI w panelu dialogowym.");
            return;
        }

        leftPortrait = left.GetComponent<Image>();
        rightPortrait = right.GetComponent<Image>();
        dialogueText = text.GetComponent<TMP_Text>();

        dialoguePanelInstance.SetActive(false);
    }

    currentDialogue = FindDialogueByName(dialogueName);
    if (currentDialogue == null)
    {
        Debug.LogWarning($"Dialog o nazwie '{dialogueName}' nie został znaleziony!");
        return;
    }

    currentLine = 0;
    dialoguePanelInstance.SetActive(true);
    ShowLine(currentLine);
    currentLine++;
}


    private DialogueData FindDialogueByName(string name)
    {
        foreach (var d in allDialogues)
        {
            if (d.dialogueName == name)
                return d;
        }
        return null;
    }

    public void ShowLine(int index)
    {
        if (index >= currentDialogue.dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = currentDialogue.dialogueLines[index];
        Character speaker = GetCharacterByType(line.character);

        if (speaker == null) return;

        if (line.isLeftSide)
        {
            leftPortrait.sprite = speaker.portrait;
            leftPortrait.color = Color.white;
            rightPortrait.color = new Color(1, 1, 1, 0);
        }
        else
        {
            rightPortrait.sprite = speaker.portrait;
            rightPortrait.color = Color.white;
            leftPortrait.color = new Color(1, 1, 1, 0);
        }

        dialogueText.text = $"{speaker.name}: {line.text}";
    }

    private Character GetCharacterByType(CharacterType type)
    {
        foreach (Character c in currentDialogue.characters)
        {
            if (c.type == type)
                return c;
        }
        Debug.LogWarning("Brak postaci o typie: " + type);
        return null;
    }

    private void EndDialogue()
    {
        dialoguePanelInstance.SetActive(false);
        Debug.Log("Dialog zakończony.");
    }
public void NextLine()
{
    if (currentDialogue == null)
    {
        Debug.LogWarning("Nie załadowano żadnego dialogu.");
        return;
    }

    if (currentLine >= currentDialogue.dialogueLines.Length)
    {
        EndDialogue();
        return;
    }

    ShowLine(currentLine);
    currentLine++;
}
public GameObject GetPanelInstance()
{
    return dialoguePanelInstance;
}
}
