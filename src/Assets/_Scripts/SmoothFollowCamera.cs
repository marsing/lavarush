using UnityEngine;

internal sealed class SmoothFollowCamera : MonoBehaviour {
    #region Singleton

    private static SmoothFollowCamera instance;

    internal static SmoothFollowCamera Instance {
        get {
            if(instance != null) return instance;

            instance = FindObjectOfType(typeof(SmoothFollowCamera)) as SmoothFollowCamera;
            if(instance != null) return instance;

            var container = new GameObject("SmoothFollowCamera");
            instance = container.AddComponent<SmoothFollowCamera>();
            return instance;
        }
    }

    #endregion

    #region Enumerations

    #endregion

    #region Events and Delegates

    #endregion

    #region Fields

    #region Static Fields

    #endregion

    #region Constant Fields

    #endregion

    #region Public Fields

    #endregion

    #region Private Fields

    #endregion

    #endregion

    #region Properties

    [SerializeField] private Camera targetCamera;

    internal Camera TargetCamera {
        get { return targetCamera; }
        set { targetCamera = value; }
    }

    [SerializeField] private Transform followTarget;

    internal Transform FollowTarget {
        get { return followTarget; }
        set { followTarget = value; }
    }

    [SerializeField] private Vector3 positionOffset;

    internal Vector3 PositionOffset {
        get { return positionOffset; }
        set { positionOffset = value; }
    }

    [SerializeField] private float damping;

    internal float Damping {
        get { return damping; }
        set { damping = value; }
    }

    #endregion

    #region Methods

    private void OnEnable() {
        InitializeTargetCamera();
        ResetPosition();
    }

    private void InitializeTargetCamera() {
        if(!TargetCamera) TargetCamera = Camera.main;
    }

    internal void ResetPosition() {
        if(!TargetCamera && !FollowTarget) return;

        TargetCamera.transform.position = FollowTarget.position + PositionOffset;
    }

    private void LateUpdate() {
        if(IsTargetAboveScreenCenter()) UpdatePosition();
    }

    private void UpdatePosition() {
        if(!TargetCamera && !FollowTarget) return;

        var desiredPosition = new Vector3(0, FollowTarget.position.y, 0) + PositionOffset;
        TargetCamera.transform.position = Vector3.Lerp(TargetCamera.transform.position, desiredPosition, Damping * Time.deltaTime);
    }

    private bool IsTargetAboveScreenCenter() {
        if(!TargetCamera && !FollowTarget) return false;
        return TargetCamera.WorldToScreenPoint(FollowTarget.position).y > (Screen.height / 2.0f);
    }

    internal bool IsBelowScreen(Transform targetTransform) {
        if(!TargetCamera) return false;
        return TargetCamera.WorldToScreenPoint(targetTransform.position).y < 0;
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
