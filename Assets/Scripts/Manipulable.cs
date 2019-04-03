using System;
using UnityEngine;
using UnityEngine.Events;

public class Manipulable : MonoBehaviour {
	[Serializable]
	public class TapEvent : UnityEvent<GameObject> {}
	
	[SerializeField]
	public TapEvent onTap = new TapEvent();
}
