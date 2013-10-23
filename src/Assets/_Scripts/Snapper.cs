using System;
using UnityEngine;

internal sealed class Snapper : MonoBehaviour {
    #region Enumerations

    internal enum SnapSide {
        Left,
        Right,
        Bottom,
        Top
    }

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

    [SerializeField] private SnapSide snapTo;

    internal SnapSide SnapTo1 {
        get { return snapTo; }
        set { snapTo = value; }
    }

    [SerializeField] private Vector2 offset;

    internal Vector2 Offset {
        get { return offset; }
        set { offset = value; }
    }

    #endregion

    #region Methods

    private void OnEnable() {
        SnapTo(snapTo);
    }

    private void SnapTo(SnapSide side) {
        var xPos = 0.0f;
        var yPos = 0.0f;

        switch(side) {
            case SnapSide.Left:
                xPos = Camera.main.ScreenToWorldPoint(Vector3.zero).x;
                transform.position = new Vector3(xPos + offset.x, transform.position.y + offset.y, transform.position.z);
                break;
            case SnapSide.Right:
                xPos = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
                transform.position = new Vector3(xPos + offset.x, transform.position.y + offset.y, transform.position.z);
                break;
            case SnapSide.Bottom:
                yPos = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 0)).y;
                transform.position = new Vector3(transform.position.x + offset.x, yPos + offset.y, transform.position.z);
                break;
            case SnapSide.Top:
                yPos = Camera.main.ScreenToWorldPoint(new Vector3(0, Screen.height, 0)).y;
                transform.position = new Vector3(transform.position.x + offset.x, yPos + offset.y, transform.position.z);
                break;
            default:
                throw new ArgumentOutOfRangeException("side");
        }
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
