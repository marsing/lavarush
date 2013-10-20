using UnityEngine;
using System.Collections.Generic;

internal sealed class PlatformManager : MonoBehaviour {
    #region Singleton

	private static PlatformManager instance;

	internal static PlatformManager Instance {
		get {
			if(instance != null)
				return instance;

			instance = FindObjectOfType(typeof(PlatformManager)) as PlatformManager;
			if(instance != null)
				return instance;

			var container = new GameObject("PlatformManager");
			instance = container.AddComponent<PlatformManager>();
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

	private List<GameObject> platformPool = new List<GameObject>();
	private int activatedPlatformCount;
	private float leftmostViewportPosition;
	private float rightmostViewportPosition;

    #endregion

    #endregion

    #region Properties
	
	[SerializeField]
	private GameObject platformPrefab;
	
	internal GameObject PlatformPrefab {
		get { return platformPrefab; }
		set { platformPrefab = value; }
	}
	
	[SerializeField]
	private int activePlatformCount;
	
	internal int ActivePlatformCount {
		get { return activePlatformCount; }
		set { activePlatformCount = value; }
	}
	
	[SerializeField]
	private Vector2 platformPositionOffset;
	
	internal Vector2 PlatformPositionOffset {
		get { return platformPositionOffset; }
		set { platformPositionOffset = value; }
	}
	
    #endregion

    #region Methods

	private void Start () {
		InitializeViewportPositions();
		InitializePool();
	}

	private void InitializeViewportPositions () {
		leftmostViewportPosition = Camera.main.ScreenToWorldPoint(Vector3.zero).x;
		rightmostViewportPosition = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0)).x;
	}

	private void InitializePool () {
		for(var i = 0; i < ActivePlatformCount; ++i) {
			var platform = Instantiate(platformPrefab) as GameObject;
			platform.SetActive(false);
			platform.transform.parent = transform;
			platform.transform.localPosition = Vector3.zero;
			platformPool.Add(platform);
		}
		
		for(var i = 0; i < ActivePlatformCount; ++i)
			ActivateNext();
	}
	
	private void ActivateNext () {
		var nextPlatform = platformPool[0];
		
		nextPlatform.SetActive(true);
		platformPool.RemoveAt(0);

		var randomPositionOffsetVector = new Vector3(Random.Range(-PlatformPositionOffset.x, PlatformPositionOffset.x), PlatformPositionOffset.y);
		nextPlatform.transform.position = platformPool[platformPool.Count - 1].transform.position + randomPositionOffsetVector;

		// Don't put platforms outside the viewport
		if(nextPlatform.transform.position.x < leftmostViewportPosition)
			nextPlatform.transform.position = new Vector3(leftmostViewportPosition, nextPlatform.transform.position.y, nextPlatform.transform.position.z);
		if(nextPlatform.transform.position.x > rightmostViewportPosition)
			nextPlatform.transform.position = new Vector3(rightmostViewportPosition, nextPlatform.transform.position.y, nextPlatform.transform.position.z);

		platformPool.Add(nextPlatform);
		
		// If all the platforms in the pool are used, start recycling the oldest ones to use as new
		activatedPlatformCount++;
		if(activatedPlatformCount >= ActivePlatformCount)
			RecycleOldest();
	}

	private void RecycleOldest () {
		var oldestPlatform = platformPool[0];
		
		oldestPlatform.SetActive(false);
		platformPool.Remove(oldestPlatform);
		platformPool.Insert(0, oldestPlatform);
	}

	private void Update () {
		if(Time.frameCount % 200 == 0)
			ActivateNext();
	}

    #endregion

    #region Structs

    #endregion

    #region Classes

    #endregion
}