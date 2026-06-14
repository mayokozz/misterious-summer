using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("=== Retrato NPC (imagen grande de fondo) ===")]
    public Image npcPortrait;
    public float portraitFadeSpeed = 4f;

    [Header("=== Caja de Diálogo ===")]
    public GameObject dialogueRoot;
    public GameObject dialogueBox;

    [Header("=== Texto ===")]
    public TextMeshProUGUI speakerNameText;
    public TextMeshProUGUI bodyText;
    public GameObject continueArrow;

    [Header("=== Opciones ===")]
    public GameObject choicesPanel;
    public Button[] choiceButtons;
    public TextMeshProUGUI[] choiceTexts;

    [Header("=== Typewriter ===")]
    [Range(20f, 150f)] public float typingSpeed = 50f;
    [Range(50f, 300f)] public float fastTypingSpeed = 180f;

    [Header("=== Teclas ===")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode advanceKey = KeyCode.Space;

    private Queue<DialogueLine> lineQueue = new();
    private Action<int> onChoiceCallback;
    private bool isActive = false;
    private bool isTyping = false;
    private bool skipTyping = false;
    private Coroutine typingCoroutine;
    private PlayerController player;
    private Sprite currentPortraitSprite;

    public bool IsActive => isActive;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        dialogueRoot.SetActive(false);
        choicesPanel.SetActive(false);
        if (continueArrow != null) continueArrow.SetActive(false);

        if (npcPortrait != null)
        {
            var c = npcPortrait.color;
            c.a = 0f;
            npcPortrait.color = c;
        }
    }

    void Update()
    {
        if (!isActive) return;

        if (npcPortrait != null && currentPortraitSprite != null)
        {
            var c = npcPortrait.color;
            c.a = Mathf.MoveTowards(c.a, 1f, portraitFadeSpeed * Time.deltaTime);
            npcPortrait.color = c;
        }

        if (Input.GetKeyDown(advanceKey) || Input.GetKeyDown(interactKey))
        {
            if (isTyping)
                skipTyping = true;
            else if (!choicesPanel.activeSelf)
                ShowNextLine();
        }
    }

    public void StartDialogue(DialogueData data)
    {
        if (isActive) return;

        player = FindAnyObjectByType<PlayerController>();
        if (player != null) player.enabled = false;

        lineQueue.Clear();
        foreach (var line in data.lines)
            lineQueue.Enqueue(line);

        isActive = true;
        dialogueRoot.SetActive(true);
        RegisterChoiceCallbacks(data);
        ShowNextLine();
    }

    public void EndDialogue()
    {
        isActive = false;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        StartCoroutine(FadeOutAndClose());
    }

    void ShowNextLine()
    {
        if (lineQueue.Count == 0) { EndDialogue(); return; }

        DialogueLine line = lineQueue.Dequeue();

        if (speakerNameText != null)
            speakerNameText.text = line.speakerName;

        if (npcPortrait != null && line.portrait != currentPortraitSprite)
        {
            currentPortraitSprite = line.portrait;
            npcPortrait.sprite = line.portrait;
            npcPortrait.gameObject.SetActive(line.portrait != null);
            var c = npcPortrait.color;
            c.a = 0f;
            npcPortrait.color = c;
        }

        if (continueArrow != null) continueArrow.SetActive(false);
        choicesPanel.SetActive(false);

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        skipTyping = false;
        bodyText.text = "";

        foreach (char c in line.text)
        {
            if (skipTyping) break;
            bodyText.text += c;
            float delay = Input.GetKey(advanceKey) ? 1f / fastTypingSpeed : 1f / typingSpeed;
            yield return new WaitForSeconds(delay);
        }

        bodyText.text = line.text;
        isTyping = false;
        skipTyping = false;

        if (line.choices != null && line.choices.Length > 0)
            ShowChoices(line.choices, line.onChoiceCallback);
        else if (continueArrow != null)
            continueArrow.SetActive(true);
    }

    void ShowChoices(DialogueChoice[] choices, Action<int> callback)
    {
        onChoiceCallback = callback;
        choicesPanel.SetActive(true);
        if (continueArrow != null) continueArrow.SetActive(false);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool active = i < choices.Length;
            choiceButtons[i].gameObject.SetActive(active);
            if (!active) continue;

            choiceTexts[i].text = choices[i].text;
            int idx = i;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(idx));
        }
    }

    void OnChoiceSelected(int index)
    {
        choicesPanel.SetActive(false);
        onChoiceCallback?.Invoke(index);
    }

    IEnumerator FadeOutAndClose()
    {
        if (npcPortrait != null)
        {
            float a = npcPortrait.color.a;
            while (a > 0f)
            {
                a -= portraitFadeSpeed * 2f * Time.deltaTime;
                var c = npcPortrait.color;
                c.a = Mathf.Max(0f, a);
                npcPortrait.color = c;
                yield return null;
            }
        }

        dialogueRoot.SetActive(false);
        choicesPanel.SetActive(false);
        currentPortraitSprite = null;
        if (player != null) player.enabled = true;
    }

    void RegisterChoiceCallbacks(DialogueData data)
    {
        if (data == null) return;
        foreach (var line in data.lines)
        {
            if (line.choices == null) continue;
            var choices = line.choices;
            line.onChoiceCallback = (int idx) =>
            {
                var next = choices[idx].nextDialogue;
                if (next != null) StartDialogue(next);
                else EndDialogue();
            };
        }
    }
}

