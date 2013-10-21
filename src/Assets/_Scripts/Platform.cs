using UnityEngine;

internal sealed class Platform : MonoBehaviour {
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

    private Renderer graphicsRenderer;
    private BoxCollider attachedCollider;

    #endregion

    #endregion

    #region Properties

    [SerializeField] private float minimumSize;

    internal float MinimumSize {
        get { return minimumSize; }
    }

    [SerializeField] private float maximumSize;

    internal float MaximumSize {
        get { return maximumSize; }
    }

    #endregion

    #region Methods

    private void OnEnable() {
        InitializeRequiredComponents();
        InitializeSize();
    }

    private void InitializeRequiredComponents() {
        graphicsRenderer = transform.Find("Graphics").GetComponent<MeshRenderer>();
        attachedCollider = transform.Find("Collider").GetComponent<BoxCollider>();
    }

    private void InitializeSize() {
        var randomSize = new Vector3(Random.Range(MinimumSize, MaximumSize), 2, 2);
        transform.localScale = randomSize;

        graphicsRenderer.material.SetTextureScale("_MainTex", randomSize / 2);
    }

    private void Update() {
        if(SmoothFollowCamera.Instance.IsBelowScreen(transform)) {
            PlatformManager.Instance.Recycle(gameObject);
            PlatformManager.Instance.ActivateNext();
        }

        ToggleTriggerWithPlayerPosition();
    }

    private void ToggleTriggerWithPlayerPosition() {
        // If the player is below the platform or if it's out if it's bounds in X, make it trigger otherwise collider
        attachedCollider.isTrigger = PlayerMover.Instance.transform.position.y < transform.position.y
                                     || PlayerMover.Instance.transform.position.x
                                     < (transform.position.x - transform.localScale.x / 2)
                                     || PlayerMover.Instance.transform.position.x
                                     > (transform.position.x + transform.localScale.x / 2);
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
