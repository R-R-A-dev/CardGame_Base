using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private int moveSpeed = 5;
    [SerializeField] private Animator playerAnim;
    public Rigidbody2D rb;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        rb.linearVelocity = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized * moveSpeed;
        if (rb.linearVelocity != Vector2.zero)
        {
            playerAnim.SetBool("1_Move", true);
            if (Input.GetAxisRaw("Horizontal") != 0)
            {
                if (Input.GetAxisRaw("Horizontal") > 0)
                {
                    transform.localScale = new Vector3(-1, 1, 1);
                }
                else
                {
                    transform.localScale = new Vector3(1, 1, 1);
                }
            }

        }else if(rb.linearVelocity == Vector2.zero)
        {
            playerAnim.SetBool("1_Move", false);
        }
    }
}
