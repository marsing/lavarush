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
        InitializeSize();
    }

    private void InitializeSize() {
        var randomSize = new Vector3(Random.Range(MinimumSize, MaximumSize), 1, 1);
        transform.localScale = randomSize;
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
