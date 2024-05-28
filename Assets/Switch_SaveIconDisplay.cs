using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Switch_SaveIconDisplay : MonoBehaviour
{
	public Image myImage;
	public Animatable myAnim;

	private static Switch_SaveIconDisplay _instance;
	
	public void Start()
	{
		if (_instance != null)
		{
			Destroy(gameObject);
			return;
		}

		//Debug.Log("SaveIcon Start");
		_instance = this;
		_instance.enabled = true;

	}

	public static void Hide()
	{
        //if (Debug.isDebugBuild) Debug.Log("HIDE save icon display at " + Time.realtimeSinceStartup);
        _instance.myImage.enabled = false;
	}
	
	public static void Show()
	{
        //if (Debug.isDebugBuild) Debug.Log("SHOW save icon display at " + Time.realtimeSinceStartup);
		_instance.myImage.enabled = true;
		_instance.myAnim.SetAnim("Default");
	}
}