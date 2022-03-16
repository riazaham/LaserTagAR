using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData
{
    private int player_hp;
    private int player_sp;
    private int player_gun_ammo;
    private int player_grenade_ammo;
    private int player_shield_ammo;
    private bool player_shield_status;

    public int getPlayerHP() { return this.player_hp; }
    public void setPlayerHP(int new_hp) { this.player_hp = new_hp; }

    public int getPlayerSP() { return this.player_sp; }
    public void setPlayerSP(int new_sp) { this.player_sp = new_sp; }

    public int getPlayerGunAmmo() { return this.player_gun_ammo; }
    public void setPlayerGunAmmo(int new_ammo) { this.player_gun_ammo = new_ammo; }

    public int getPlayerGrenadeAmmo() { return this.player_grenade_ammo; }
    public void setPlayerGrenadeAmmo(int new_ammo) { this.player_grenade_ammo = new_ammo; }

    public int getPlayerShieldAmmo() { return this.player_shield_ammo; }
    public void setPlayerShieldAmmo(int new_ammo) { this.player_shield_ammo = new_ammo; }

    public bool getPlayerShieldStatus() { return this.player_shield_status; }
    public void setPlayerShieldStatus(bool new_ammo) { this.player_shield_status = new_ammo; }
}
