using System;
using UnityEngine;
using Random = System.Random;

public class LuaSimulator : MonoBehaviour
{

    public PlayerModel playerModel;

    public string Execute(string action)
    {
        action = action.Trim();
        
        switch (action)
        {
            case "lua help":
                return "Lua-sim - help "
                       + "\n lua help - this text"
                       + "\n lua about - about the Lua-sim"
                       + "\n joke - tell me a joke"
                       + "\n score - output player score"
                       + "\n player.score = <n> - set player score to <n>"; 
            case "lua about":
                return "Lua-sim - about "
                       + "\n LUA simulator for client-server testing (version 0.1 - Sep 2018)";
            case "joke":
                return "Lua-sim - joke "
                       + "\n " + RandomJoke();
            case "score":
                return "Lua-sim - score "
                       + "\n player.score = " + playerModel.GetScore();
            case "player.score = <n>":
                return "Lua-sim - score "
                       + "\n player.score = " + playerModel.GetScore();
        }
        
        // player.score = <n>
        string setScoreString = "player.score = ";
        if (0 == action.IndexOf(setScoreString))
        {
            string scoreString =  action.Remove(0, setScoreString.Length);            
            return TrySetNewScore(scoreString);
        }
        

            
        // default - action not recognized
        return "Lua-sim - error - unknown command: " + action;

    }

    private string TrySetNewScore(string scoreString)
    {
        int newScore;

        try
        {
            newScore = Int32.Parse(scoreString);
            playerModel.SetScore(newScore);
            return "Lua-sim - score changed to " + playerModel.GetScore();
        }
        catch (FormatException)
        {
            return "Lua-sim - error - invalid score value (must be integer): " + scoreString;
        }
        
    }

    private string RandomJoke()
    {
        string[] jokes = new string[]
        {
            "knock knock \n who's there \n Isabelle \n Isabelle who? \n is a bell necessary if you have a knocker",
            "timing \n what's the secret of a good joke"
        };
        
        Random random = new Random();
        int jokeIndex = random.Next(0, jokes.Length);
        return jokes[jokeIndex];
    }


}
