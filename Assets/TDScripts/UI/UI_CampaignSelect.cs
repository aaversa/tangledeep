using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Rewired;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Class that controls the selection between the base campaign and Shara Mode
/// </summary>
public class UI_CampaignSelect : MonoBehaviour
{
	enum SelectionState
	{
		none = 0,
		mirai,
		shara,
		max
	}
	
	[Tooltip("The Mirai border image")]
	[SerializeField]
	private Image imgSelectMirai;
	
	[Tooltip("The Shara border image")]
	[SerializeField]
	private Image imgSelectShara;

	[Tooltip("Header for this screen.")]
	[SerializeField]
	private TextMeshProUGUI labelTitle;

	[Tooltip("Text that displays information about the selected mode.")]
	[SerializeField]
	private TextMeshProUGUI labelExplainMode;
	
	[Tooltip("Choose your friends.")]
	[SerializeField]
	private TextMeshProUGUI labelSelectMirai;

	[Tooltip("Choose your destiny.")]
	[SerializeField]
	private TextMeshProUGUI labelSelectShara;

	[Header("Image movement")]
	[Tooltip("Pixel distance from 0 that an image will retreat to when not selected.")]
	[SerializeField]
	private float pxDistanceFromBorder;
	
	[Tooltip("PX per second that a selected image advances.")]
	[SerializeField]
	private float pxAdvancePerSecond;

	[Tooltip("PX per second that a selected image retreats.")]
	[SerializeField]
	private float pxRetreatPerSecond;

	/// <summary>
	/// Current choice, starts with unchosen.
	/// </summary>
	private SelectionState currentChoice;

	/// <summary>
	/// The gametime when were opened. We use this to ignore the first second's worth of mouseovers.
	/// </summary>
	private float menuStartTime;

    /// <summary>
    /// Don't start processing input for a few frames after opening this, so one confirm on previous screen doesn't carry over here.
    /// </summary>
    private void OnEnable()
    {
        menuStartTime = Time.realtimeSinceStartup;
    }

    // Use this for initialization
    void Start () 
	{
		//make sure label has correct info
		imgSelectMirai.material = Instantiate(imgSelectMirai.material);
		imgSelectShara.material = Instantiate(imgSelectShara.material);

		//what the screen is
		labelTitle.text = StringManager.GetString("exp_campaign_select_title");

		//starts empty
		labelExplainMode.text = "";

		labelSelectMirai.text = StringManager.GetString("exp_campaign_select_label_mirai");
		labelSelectShara.text = StringManager.GetString("exp_campaign_select_label_shara");

        FontManager.LocalizeMe(labelSelectMirai, TDFonts.WHITE);
        FontManager.LocalizeMe(labelSelectShara, TDFonts.WHITE);
        FontManager.LocalizeMe(labelTitle, TDFonts.WHITE);
        FontManager.LocalizeMe(labelExplainMode, TDFonts.WHITE);

        menuStartTime = Time.realtimeSinceStartup;
		
		//make sure we're not still in shara mode if we came here via backing up from another menu.
		GameStartData.gameInSharaMode = false;
	}
	
	// Update is called once per frame
	void Update () 
	{
		//ensure that dialog cursor is facing correct option
		GameObject selectedObject = null;

		switch (currentChoice)
		{
			case SelectionState.mirai:
				selectedObject = labelSelectMirai.gameObject;
				break;
			case SelectionState.shara:
				selectedObject = labelSelectShara.gameObject;
				break;
			case SelectionState.max:
			case SelectionState.none:
				break;
		}

		if (selectedObject == null)
		{
			UIManagerScript.HideDialogMenuCursor();
		}
		else
		{
			//grab the cursor and put it where we want it.
			UIManagerScript.ShowDialogMenuCursor();
			UIManagerScript.singletonUIMS.uiDialogMenuCursor.gameObject.SetActive(true);
			UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(transform);
			UIManagerScript.AlignCursorPos( UIManagerScript.singletonUIMS.uiDialogMenuCursor,
											selectedObject, 0, 0f, false);
		}
		
	}
	

	/// <summary>
	/// Call whenever we suspect we have changed selection modes.
	/// </summary>
	/// <param name="newSelection">The state we want to change to</param>
	/// <returns>TRUE if there was a legit change, FALSE if it the state we were already in.</returns>
	bool OnSelectModeChange(SelectionState newSelection)
	{
		//don't change if we are already here
		if (newSelection == currentChoice)
		{
			return false;
		}

		currentChoice = newSelection;
		
		//kill the coroutines if they are running
		StopAllCoroutines();

		if (newSelection == SelectionState.mirai)
		{
			StartCoroutine(ActivateSelection(imgSelectMirai));
			StartCoroutine(DeactivateSelection(imgSelectShara));
			labelExplainMode.text =  StringManager.GetString("exp_campaign_select_info_mirai");
		}
		else if (newSelection == SelectionState.shara)
		{
			StartCoroutine(ActivateSelection(imgSelectShara));
			StartCoroutine(DeactivateSelection(imgSelectMirai));
			labelExplainMode.text =  StringManager.GetString("exp_campaign_select_info_shara");
			
		}
		return true;
	}
	
	/// <summary>
	/// Colorize the image, and move it to X==0
	/// </summary>
	/// <param name="activateMe"></param>
	/// <returns></returns>
	IEnumerator ActivateSelection(Image activateMe)
	{
		var rt = activateMe.rectTransform;
		float goal = 0f;
		float startPosition = rt.anchoredPosition.x;
		
		float moveDelta = pxAdvancePerSecond;

		//Shara's image is >0 when deselected, so reduce her to 0.
		bool positionStartsBelowZero = true;
		if (activateMe == imgSelectShara)
		{
			positionStartsBelowZero = false;
			moveDelta *= -1.0f;
		}
		
		//Move towards the goal
		var rtCurrent = Vector2.zero;
		while ( positionStartsBelowZero && rt.anchoredPosition.x < goal ||
		        !positionStartsBelowZero && rt.anchoredPosition.x > goal)
		{
			rtCurrent = rt.anchoredPosition;
			rtCurrent.x += moveDelta * Time.deltaTime;
			rt.anchoredPosition = rtCurrent;
			yield return null;
			
			//the farther you are from the goal, the greyer you should be.
			var deltaFromGoal = Mathf.Abs(goal - rtCurrent.x);
			
			//we know the goal is 0, so our ratio is distance from start
			var ratio = Mathf.Abs(deltaFromGoal / startPosition);
			
			activateMe.material.SetFloat("_GrayScale", ratio);

		}
		
		//lock into the goal here.
		rtCurrent = rt.anchoredPosition;
		rtCurrent.x = goal;
		rt.anchoredPosition = rtCurrent;
		activateMe.material.SetFloat("_GrayScale", 0);
		
	}
	
	/// <summary>
	/// Decolorize the image, move it to pxDistanceFromBorder away from 0.
	/// Will be a negative number for Mirai.
	/// </summary>
	/// <param name="deactivateMe"></param>
	/// <returns></returns>
	IEnumerator DeactivateSelection(Image deactivateMe)
	{
		var rt = deactivateMe.rectTransform;
		float goal = pxDistanceFromBorder;
		float startValue = rt.anchoredPosition.x;
		float moveDelta = pxRetreatPerSecond;

		//Mirai's image is <0 when deselected, so reduce her to the value.
		if (deactivateMe == imgSelectMirai)
		{
			moveDelta *= -1.0f;
			goal *= -1.0f;
		}
		
		//Move towards the goal
		var rtCurrent = Vector2.zero;
		while ( goal > 0 && rt.anchoredPosition.x < goal ||
		        goal < 0 && rt.anchoredPosition.x > goal)
		{
			rtCurrent = rt.anchoredPosition;
			rtCurrent.x += moveDelta * Time.deltaTime;
			rt.anchoredPosition = rtCurrent;

			//the farther you are from the goal, the greyer you should be.
			var deltaFromGoal = Mathf.Abs(goal - rtCurrent.x);
			
			//we know the goal is non zero
			var ratio = Mathf.Abs(deltaFromGoal / goal);
			ratio = 1.0f - ratio;
			
			deactivateMe.material.SetFloat("_GrayScale", ratio);
			
			yield return null;
		}
		
		//lock into the goal here.
		rtCurrent = rt.anchoredPosition;
		rtCurrent.x = goal;
		rt.anchoredPosition = rtCurrent;
		deactivateMe.material.SetFloat("_GrayScale", 1);

	}
	
	
	/// <summary>
	/// Handles keyboard / joystick input for this mode.
	///	</summary>
	/// <returns>TRUE if the input was consumed and nothing else needs to be processed.</returns>
	public bool UpdateInput()
	{
        /* if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log("Updating input on campaign select.");
        } */
        
        if (Time.realtimeSinceStartup - menuStartTime < 0.2f)
        {
            return true;
        }

		var playerInput = ReInput.players.GetPlayer(0); 
		
		var verticalAxis = playerInput.GetAxis("Move Vertical");
		var horizontalAxis = playerInput.GetAxis("Move Horizontal");
		
		bool moveUp = playerInput.GetButtonDown("Move Up");
		bool moveDown = playerInput.GetButtonDown("Move Down");
		bool moveLeft = playerInput.GetButtonDown("Move Left");
		bool moveRight = playerInput.GetButtonDown("Move Right");

		//up or left selects mirai
		if (verticalAxis > 0f || horizontalAxis < 0f || moveUp || moveLeft)
		{
            //Debug.Log("Up or left.");
			if (OnSelectModeChange(SelectionState.mirai))
			{
				UIManagerScript.PlayCursorSound("Move");
			}
			return true;
		}
		//down or right selects shara
		if (verticalAxis < 0 || horizontalAxis > 0 || moveDown || moveRight)
		{
            //Debug.Log("Down or right.");
            if ( OnSelectModeChange(SelectionState.shara))
			{
				UIManagerScript.PlayCursorSound("Move");
			}
			return true;
		}

		//confirm does a thing
		if (playerInput.GetButtonDown("Confirm"))
		{
			ConfirmCampaignChoice(currentChoice);
			return true;
		}
		
		//back does a thing
		return true;
	}

	//on mouse over 
	public void OnImageMouseOver(int idx)
	{
		//Ignore mouseovers briefly so that the very first frame doesn't pick a side because
		//the mouse happens to be there.
		if (Time.realtimeSinceStartup - menuStartTime < 0.2f)
		{
			return;
		}
		
		if (idx == 0)
		{
			OnSelectModeChange(SelectionState.mirai);
		}
		else if (idx == 1)
		{
			OnSelectModeChange(SelectionState.shara);
		}
	}
	
	//on mouse click -- pick something
	public void OnImageMouseClick(int idx)
	{
		//Also soak the first 1/5th of a second when the menu opens up so that no one can just click through
		//accidentally. 
		if (Time.realtimeSinceStartup - menuStartTime < 0.2f)
		{
			return;
		}
		
		if (idx == 0)
		{
			ConfirmCampaignChoice(SelectionState.mirai);
		}
		else if (idx == 1)
		{
			ConfirmCampaignChoice(SelectionState.shara);
		}
	}


	/// <summary>
	/// This will launch one of the two campaigns!
	/// </summary>
	/// <param name="pickMe"></param>
	void ConfirmCampaignChoice(SelectionState pickMe)
	{
		//this is not ok.
		if (pickMe == SelectionState.max || pickMe == SelectionState.none)
		{
			return;
		}
		
		//go through the closing process for this object.
		gameObject.SetActive(false);
		
		//play a sound
		
		//hand the dialog cursor back to the dialog box
		UIManagerScript.singletonUIMS.uiDialogMenuCursor.transform.SetParent(
			UIManagerScript.myDialogBoxComponent.transform );
		
		//pick where to go next
		if (pickMe == SelectionState.mirai)
		{
			GameStartData.gameInSharaMode = false;
            GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = false;
            UIManagerScript.singletonUIMS.StartCharacterCreation_Mirai();
			//UIManagerScript.singletonUIMS.StartCharacterCreation_ChooseChallengesForMirai();
			return;
		}

        //shara -- no challenges currently. 
        GameStartData.gameInSharaMode = true;
        GameStartData.slotInSharaMode[GameStartData.saveGameSlot] = true;
        CharCreation.SetGameStartDataForSharaMode();
		UIManagerScript.singletonUIMS.StartCharacterCreation_DifficultyModeSelect();
	}
	
}
