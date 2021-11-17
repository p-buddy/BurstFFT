using UnityEngine;
using System.Collections;

namespace JamUp.UnityUtility
{
    public class CameraController : MonoBehaviour
    {
        public float mainSpeed = 10.0f;   // Regular speed
        public float shiftAdd  = 25.0f;   // Amount to accelerate when shift is pressed
        public float maxShift  = 100.0f;  // Maximum speed when holding shift
        public float camSens   = 0.15f;   // Mouse sensitivity

        private Vector3 lastMouse = new Vector3(255, 255, 255);  // kind of in the middle of the screen, rather than at the top (play)
        private float totalRun = 1.0f;

        void FixedUpdate()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                lastMouse = Input.mousePosition - lastMouse;
                lastMouse = new Vector3(-lastMouse.y * camSens, lastMouse.x * camSens, 0);
                lastMouse = new Vector3(transform.eulerAngles.x + lastMouse.x, transform.eulerAngles.y + lastMouse.y, 0);
                transform.eulerAngles = lastMouse;
                lastMouse = Input.mousePosition;
            }
            
            Vector3 velocity = GetBaseInput();
            if (Input.GetKey(KeyCode.LeftShift))
            {
                totalRun += Time.fixedDeltaTime;
                velocity *= totalRun * shiftAdd;
                velocity.x = Mathf.Clamp(velocity.x, -maxShift, maxShift);
                velocity.y = Mathf.Clamp(velocity.y, -maxShift, maxShift);
                velocity.z = Mathf.Clamp(velocity.z, -maxShift, maxShift);
            }
            else
            {
                totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                velocity *= mainSpeed;
            }

            velocity *= Time.fixedDeltaTime;
            transform.Translate(velocity);
        }

        private Vector3 GetBaseInput()
        {
            Vector3 velocity = new Vector3();

            // Forwards / Backwards
            velocity += Input.GetKey(KeyCode.W) ? Vector3.forward : Vector3.zero;
            velocity += Input.GetKey(KeyCode.S) ? -Vector3.forward : Vector3.zero;
            
            // Left / Right
            velocity += Input.GetKey(KeyCode.A) ? -Vector3.right : Vector3.zero;
            velocity += Input.GetKey(KeyCode.D) ? Vector3.right : Vector3.zero;

            // Up / Down
            velocity += Input.GetKey(KeyCode.E) ? Vector3.up : Vector3.zero;
            velocity += Input.GetKey(KeyCode.Q) ? -Vector3.up : Vector3.zero;

            return velocity;
        }
    }
}
