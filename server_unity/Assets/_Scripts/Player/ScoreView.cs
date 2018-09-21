using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class ScoreView : MonoBehaviour
{
	public Text textScore;
	public PlayerModel playerModel;

	// Use this for initialization
	void Start ()
	{
		UpdateScoreDisplay();

	}
	
	// Update is called once per frame
	void Update ()
	{
		UpdateScoreDisplay();

	}

	private void UpdateScoreDisplay()
	{
		int score = playerModel.GetScore();
		textScore.text = "score = " + score;

	}
}
