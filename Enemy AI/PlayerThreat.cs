using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerThreat
{
    public int playerID;
    public int threatAmount;

    public PlayerThreat(int playerid, int dmgAmount)
    {
        playerID = playerid;
        threatAmount = dmgAmount;
    }
}
