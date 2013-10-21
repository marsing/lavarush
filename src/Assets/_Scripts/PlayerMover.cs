using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
internal sealed class PlayerMover : MonoBehaviour {
    #region Singleton

    private static PlayerMover instance;

    internal static PlayerMover Instance {
        get {
            if(instance != null)
                return instance;

            instance = FindObjectOfType(typeof(PlayerMover)) as PlayerMover;
            if(instance != null)
                return instance;

            var container = new GameObject("PlayerMover");
            instance = container.AddComponent<PlayerMover>();
            return instance;
        }
    }

    #endregion

    #region Enumerations

    #endregion

    #region Events and Delegates

    // Occurs when a finger touch the screen hover the joystick
    private void On_JoystickTouchStart(MovingJoystick move) {}

    // Occurs when joystick is starting to move.
    private void On_JoystickMoveStart(MovingJoystick move) {}

    // Occurs when joystick is moving.
    private void On_JoystickMove(MovingJoystick move) {}

    // Occurs when joystick is ending to move,
    private void On_JoystickMoveEnd(MovingJoystick move) {}

    // Occurs when a finger was lifted from the joystick , and the time elapsed since the beginning of the touch is less than the time required for the detection of a long tap
    private void On_JoystickTap(MovingJoystick move) {}

    // Occurs when the number of taps is egal to 2 in a short time hover the joystick
    private void On_JoystickDoubleTap(MovingJoystick move) {}

    // Occurs when a finger hover the joystick was lifted from the screen.
    private void On_JoystickTouchUp(MovingJoystick move) {}

    // Occurs when the button is down for the first time.
    private void On_ButtonDown(string buttonName) {
        switch(buttonName) {
            case "ButtonJump":
                Jump(JumpForce);
                break;
        }
    }

    // Occurs when the button is pressed
    private void On_ButtonPress(string buttonName) {
        switch(buttonName) {
            case "ButtonLeft":
                Move(-1f);
                break;
            case "ButtonRight":
                Move(1f);
                break;
        }
    }

    // Occurs when the button is up
    private void On_ButtonUp(string buttonName) {}

    #endregion

    #region Fields

    #region Static Fields

    #endregion

    #region Constant Fields

    private const float isGroundedHeight = 0.5f;

    #endregion

    #region Public Fields

    #endregion

    #region Private Fields

    #endregion

    #endregion

    #region Properties

    [SerializeField] private float movementSpeed;

    internal float MovementSpeed {
        get { return movementSpeed; }
        set { movementSpeed = value; }
    }

    [SerializeField] private float airMovementSpeed;

    internal float AirMovementSpeed {
        get { return airMovementSpeed; }
        set { airMovementSpeed = value; }
    }

    [SerializeField] private float jumpForce;

    internal float JumpForce {
        get { return jumpForce; }
        set { jumpForce = value; }
    }

    [SerializeField] private float gravityCoeff;

    internal float GravityCoeff {
        get { return gravityCoeff; }
        set { gravityCoeff = value; }
    }

    internal bool IsGrounded { get; set; }

    #endregion

    #region Methods

    private void OnEnable() {
        ToggleEvents(isRegister: true);
    }

    private void OnDisable() {
        ToggleEvents(isRegister: false);
    }

    private void OnDestroy() {
        ToggleEvents(isRegister: false);
    }

    private void ToggleEvents(bool isRegister) {
        if(isRegister) {
            EasyJoystick.On_JoystickTouchStart += On_JoystickTouchStart;
            EasyJoystick.On_JoystickMoveStart += On_JoystickMoveStart;
            EasyJoystick.On_JoystickMove += On_JoystickMove;
            EasyJoystick.On_JoystickMoveEnd += On_JoystickMoveEnd;
            EasyJoystick.On_JoystickTap += On_JoystickTap;
            EasyJoystick.On_JoystickDoubleTap += On_JoystickDoubleTap;
            EasyJoystick.On_JoystickTouchUp += On_JoystickTouchUp;

            EasyButton.On_ButtonDown += On_ButtonDown;
            EasyButton.On_ButtonPress += On_ButtonPress;
            EasyButton.On_ButtonUp += On_ButtonUp;
        } else {
            EasyJoystick.On_JoystickTouchStart -= On_JoystickTouchStart;
            EasyJoystick.On_JoystickMoveStart -= On_JoystickMoveStart;
            EasyJoystick.On_JoystickMove -= On_JoystickMove;
            EasyJoystick.On_JoystickMoveEnd -= On_JoystickMoveEnd;
            EasyJoystick.On_JoystickTap -= On_JoystickTap;
            EasyJoystick.On_JoystickDoubleTap -= On_JoystickDoubleTap;
            EasyJoystick.On_JoystickTouchUp -= On_JoystickTouchUp;

            EasyButton.On_ButtonDown -= On_ButtonDown;
            EasyButton.On_ButtonPress -= On_ButtonPress;
            EasyButton.On_ButtonUp -= On_ButtonUp;
        }
    }

    private void Move(float value) {
        float movementForce;

        if(IsGrounded)
            movementForce = value * MovementSpeed * Time.deltaTime;
        else
            movementForce = value * AirMovementSpeed * Time.deltaTime;

        rigidbody.AddForce(new Vector3(movementForce, 0, 0), ForceMode.VelocityChange);
    }

    private void Jump(float force) {
        if(!IsGrounded)
            return;

        var jumpVector = new Vector3(0, force, 0);
        rigidbody.AddForce(jumpVector, ForceMode.VelocityChange);
    }

    private void Update() {
        UpdateIsGroundedStatus();

#if !UNITY_ANDROID || !UNITY_IPHONE
        UpdateEditorInput();
#endif

        if(SmoothFollowCamera.Instance.IsBelowScreen(transform)) Application.LoadLevel(Application.loadedLevel);
    }

    private void UpdateEditorInput() {
        var movementVector = new Vector2(Input.GetAxis("Horizontal"), 0);
        Move(movementVector.x);

        if(Input.GetKeyDown(KeyCode.Space))
            Jump(JumpForce);
    }

    private void FixedUpdate() {
        if(!IsGrounded)
            AmplifyGravity();
    }

    private void UpdateIsGroundedStatus() {
        // For debugging purposes
        Debug.DrawRay(transform.position, Vector3.down * isGroundedHeight);

        // Cast the first ray directly below, return if successful
        var isGroundedRay = new Ray(transform.position + Vector3.up * 0.5f, Vector3.down);

        if(Physics.Raycast(isGroundedRay, distance: isGroundedHeight)) {
            IsGrounded = true;
            return;
        }

        // Cast the second ray towards left return if successful
        isGroundedRay = new Ray(transform.position + new Vector3(-0.5f, 0.5f, 0f), Vector3.down);

        if(Physics.Raycast(isGroundedRay, distance: isGroundedHeight)) {
            IsGrounded = true;
            return;
        }

        // Cast the third ray towards right return if successful
        isGroundedRay = new Ray(transform.position + new Vector3(0.5f, 0.5f, 0f), Vector3.down);

        if(Physics.Raycast(isGroundedRay, distance: isGroundedHeight)) {
            IsGrounded = true;
            return;
        }

        IsGrounded = false;
    }

    private void AmplifyGravity() {
        rigidbody.AddForce(Vector3.up * GravityCoeff, ForceMode.VelocityChange);
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
