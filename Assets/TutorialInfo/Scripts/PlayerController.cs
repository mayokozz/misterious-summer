using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(
            h + v,
            0,
            v - h
        );

        transform.position +=
            move.normalized *
            speed *
            Time.deltaTime;
    }
}