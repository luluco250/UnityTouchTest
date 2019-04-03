using System;
using UnityEngine;

public class Test : MonoBehaviour {
	public void OnTap(GameObject go) {
		string today = DateTime.Now.ToString("dddd");
		Debug.Log("It is " + today.ToLower() + " my dudes");
	}
}
