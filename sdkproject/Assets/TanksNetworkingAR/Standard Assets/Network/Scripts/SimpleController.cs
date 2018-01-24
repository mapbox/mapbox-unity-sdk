using UnityEngine;
using UnityEngine.Networking;
using System.Collections;


namespace UnityStandardAssets.Network
{
    //A very simple networked controller. Just show how to communicate server <-> client
    [RequireComponent(typeof(NetworkTransform))]
    public class SimpleController : NetworkBehaviour
    {

        public bool verticalMove = true;
        public bool horizontalMove = true;

        float moveX = 0;
        float moveY = 0;
        float moveSpeed = 0.2f;

        void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            // input handling for local player only
            float oldMoveX = moveX;
            float oldMoveY = moveY;

            moveX = 0;
            moveY = 0;


            if (horizontalMove)
            {
                moveX = CrossPlatformInput.CrossPlatformInputManager.GetAxis("Horizontal");
            }

            if (verticalMove)
            {
                moveY = CrossPlatformInput.CrossPlatformInputManager.GetAxis("Vertical");
            }

            if (moveX != oldMoveX || moveY != oldMoveY)
            {
                CmdMove(moveX, moveY);
            }
        }

        [Command]
        public void CmdMove(float x, float y)
        {
            Move(x, y);
        }

        public void Move(float x, float y)
        {
            moveX = x;
            moveY = y;
        }

        public void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                transform.Translate(moveX * moveSpeed, moveY * moveSpeed, 0);
            }
        }
    }
}
