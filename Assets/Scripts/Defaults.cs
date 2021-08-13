using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Defaults
{
    #region Player

    public enum PlayerStances
    {
        Stand,
        Crouch
    }

    [Serializable]
    public class PlayerSettings
    {
        [Header("View Settings")]
        public float lookXSensitivity;
        public float lookYSensitivity;

        public bool lookXInverted;
        public bool lookYInverted;

        [Header("Movement Settings")]
        public float movementSmoothing;
        public float fallSmoothing;
        public bool sprintHold;

        [Header("Movement - Walk")]
        public float walkForwardSpeed;
        public float walkBackwardSpeed;
        public float walkStrafeSpeed;

        [Header("Movement - Sprint")]
        public float sprintForwardSpeed;
        public float sprintStrafeSpeed;

        [Header("Jump")]
        public float jumpHeight;
        public float jumpFalloff;

        [Header("Speed Modifiers")]
        public float speedModifier;
        public float crouchSpeedModifier;
        public float fallSpeedModifier;
    }

    [Serializable]
    public class PlayerStance
    {
        public float CameraHeight;
        public CapsuleCollider collider;
    }

    #endregion
}
