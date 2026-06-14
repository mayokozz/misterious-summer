using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class DialogueChoice
{
    public string text = "¿Qué pasa?";
    public DialogueData nextDialogue;
}

[System.Serializable]
public class DialogueLine
{
    public string speakerName = "NPC";
    [TextArea(2, 5)]
    public string text = "Hola, viajero.";
    public Sprite portrait;
    public DialogueChoice[] choices;
    [System.NonSerialized] public Action<int> onChoiceCallback;
}

[CreateAssetMenu(fileName = "Dialogue_NPC", menuName = "Game/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueId = "NPC_01";
    public List<DialogueLine> lines = new List<DialogueLine>();
}
