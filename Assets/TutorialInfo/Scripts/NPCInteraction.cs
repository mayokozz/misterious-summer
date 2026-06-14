using UnityEngine;

public class NPCInteraction : MonoBehaviour
{
    [Header("=== Dialogo ===")]
    public DialogueData dialogueData;

    [Header("=== Proximidad ===")]
    public float interactRadius = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("=== Prompt [E] sobre el NPC ===")]
    public GameObject interactPrompt;

    private Transform playerTf;
    private bool inRange;

    void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) playerTf = p.transform;
        if (interactPrompt != null) interactPrompt.SetActive(false);
        
    }

    void Update()
    {
        if (playerTf == null) return;

        inRange = Vector3.Distance(transform.position, playerTf.position) <= interactRadius;

        if (interactPrompt != null)
            interactPrompt.SetActive(inRange && !DialogueManager.Instance.IsActive);
        if (Input.GetKeyDown(interactKey))
        {
            Debug.Log("E presionada. inRange=" + inRange + " | DialogueManager.Instance=" + (DialogueManager.Instance != null));
        }

        if (inRange && Input.GetKeyDown(interactKey) && !DialogueManager.Instance.IsActive)
            DialogueManager.Instance.StartDialogue(dialogueData);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}