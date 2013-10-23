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

    private BoxCollider attachedCollider;
    private GameObject[] platforms;

    #endregion

    #endregion

    #region Properties

    #endregion

    #region Methods

    private void OnEnable() {
        InitializeRequiredComponents();
        PopulatePlatformsArray();
        InitializePlatform();
    }

    private void InitializeRequiredComponents() {
        attachedCollider = transform.Find("Collider").GetComponent<BoxCollider>();
    }

    private void PopulatePlatformsArray() {
        var graphics = transform.Find("Graphics");
        platforms = new GameObject[graphics.childCount];

        for(var platformIndex = 0; platformIndex < platforms.Length; platformIndex++) {
            platforms[platformIndex] = graphics.GetChild(platformIndex).gameObject;
            platforms[platformIndex].SetActive(false);
        }
    }

    private void InitializePlatform() {
        var randomPlatform = Random.Range(0, platforms.Length);
        platforms[randomPlatform].SetActive(true);

        var scaleDice = Random.Range(-1, 1);
        platforms[randomPlatform].transform.localScale = new Vector3(scaleDice == -1 ? -1 : 1, 1, 1);

        // Resize the collider to match the platform size
        attachedCollider.size = new Vector3(((randomPlatform + 1) * 2), 1, 1);
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
        attachedCollider.isTrigger = PlayerMover.Instance.transform.position.y < attachedCollider.transform.position.y
                                     || PlayerMover.Instance.transform.position.x
                                     < (attachedCollider.transform.position.x - attachedCollider.size.x / 2)
                                     || PlayerMover.Instance.transform.position.x
                                     > (attachedCollider.transform.position.x + attachedCollider.size.x / 2);
    }

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}
