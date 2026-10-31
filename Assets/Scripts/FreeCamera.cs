using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


public class FreeCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 30f;
    [SerializeField] private float fastMultiplier = 4f;
    [SerializeField] private float lookSensitivity = 0.15f;

    private float yaw, pitch;
    private Vector2 lastMouse;

    private void Start()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x > 180f ? e.x - 360f : e.x;
    }

    private void Update()
    {
        bool looking;
        Vector2 look;
        Vector3 move;
        bool fast;
        float scroll;


        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (kb == null || mouse == null) return;

        looking = mouse.rightButton.isPressed;
        look = mouse.delta.ReadValue();
		move = new Vector3(
			(kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f),
			(kb.spaceKey.isPressed ? 1f : 0f) - (kb.leftCtrlKey.isPressed ? 1f : 0f),
			(kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f));
        fast = kb.leftShiftKey.isPressed;
        scroll = mouse.scroll.ReadValue().y / 120f;


        if (!looking) return;

        yaw += look.x * lookSensitivity;
        pitch = Mathf.Clamp(pitch - look.y * lookSensitivity, -89f, 89f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        moveSpeed = Mathf.Clamp(moveSpeed * (1f + scroll * 0.1f), 1f, 500f);

        float speed = moveSpeed * (fast ? fastMultiplier : 1f);
        transform.position += transform.TransformDirection(move) * (speed * Time.unscaledDeltaTime);
    }
}
