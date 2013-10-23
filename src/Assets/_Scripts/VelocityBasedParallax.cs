using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
internal sealed class VelocityBasedParallax : MonoBehaviour {
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

    [SerializeField] private Camera velocityTarget;

    internal Camera VelocityTarget {
        get { return velocityTarget; }
        set { velocityTarget = value; }
    }

    [SerializeField] private float velocityParallaxFactor;

    internal float VelocityParallaxFactor {
        get { return velocityParallaxFactor; }
        set { velocityParallaxFactor = value; }
    }

    #endregion

    #region Methods

    private void OnEnable() {
        if(VelocityTarget == null) VelocityTarget = Camera.main;
    }

    private void Update() {
        UpdateParallaxVelocity();
    }

    private void UpdateParallaxVelocity() {
        rigidbody.velocity = VelocityTarget.velocity * VelocityParallaxFactor;
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
