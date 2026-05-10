using UnityEngine;

public class StoryNPCDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueConversation conversation;

    [Header("Interaction Feel")]
    public bool facePlayerDuringDialogue = true;
    public float facePlayerTurnSpeed = 8f;

    private Transform currentPlayer;

    public DialogueConversation GetConversation()
    {
        return conversation;
    }

    public void StartFacingPlayer(Transform player)
    {
        currentPlayer = player;
    }

    public void StopFacingPlayer()
    {
        currentPlayer = null;
    }

    private void Update()
    {
        if (!facePlayerDuringDialogue)
        {
            return;
        }

        if (currentPlayer == null)
        {
            return;
        }

        Vector3 direction = currentPlayer.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * facePlayerTurnSpeed
        );
    }
}