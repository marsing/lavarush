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
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
