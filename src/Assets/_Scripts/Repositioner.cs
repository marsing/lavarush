using UnityEngine;

internal sealed class Repositioner : MonoBehaviour {
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

    [SerializeField] private int instanceCount;

    internal int InstanceCount {
        get { return instanceCount; }
        set { instanceCount = value; }
    }

    [SerializeField] private float offset;

    internal float Offset {
        get { return offset; }
        set { offset = value; }
    }

    #endregion

    #region Methods

    private void Update() {
        RepositionOnBelowScreen();
    }

    private void RepositionOnBelowScreen() {
        if(SmoothFollowCamera.Instance.IsBelowScreen(transform))
            transform.position += Vector3.up * InstanceCount * Offset;
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
