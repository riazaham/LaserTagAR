using System;

[Serializable]
public class PlayerData
{  
    public int hp;
    public string action;
    public int bullets;
    public int grenades;
    public float shield_time;
    public int shield_health;
    public int num_deaths;
    public int num_shield;
}

[Serializable]
public class Players {
    public PlayerData p1;
    public PlayerData p2;
}
