using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpponentScript : MonoBehaviour
{
    //Players buffer
    public Queue playersQueue = new Queue();
    PlayerData opponent;

    // =============== ASSETS ============= //
    // health and shield assets
    public Image hpBar, shieldBar;
    public Text hpText, shieldText;
    public GameObject shieldARObject;

    // ammo assets
    public GameObject grenadePrefab;

    // effect assets
    public ParticleSystem grenadeExplosionPrefab;
    public AnimationCurve grenadeParabolaCurve;

    // player and opponent location asset
    public Camera ARCamera;
    public GameObject imageTarget;

    // AR UI Canvas
    public Canvas ARUICanvas;


    // ============ FLAGS AND VARIABLES ============= //
    private IEnumerator shieldTimer;

    // =============== Create singleton instance ============== //
    private static OpponentScript _instance;
    public static OpponentScript Opponent { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;

        string state = "{\"p1\": {\"hp\": 70, \"action\": \"none\", \"bullets\": 3, \"grenades\": 1, \"shield_time\": 0, \"shield_health\": 20, \"num_deaths\": 0, \"num_shield\": 1}, \"p2\": {\"hp\": 90, \"action\": \"none\", \"bullets\": 6, \"grenades\": 2, \"shield_time\": 0, \"shield_health\": 30, \"num_deaths\": 0, \"num_shield\": 3}}";
        Players players = JsonUtility.FromJson<Players>(state);
        this.playersQueue.Enqueue(players.p2);
    }

    // ============ ROTATE TOWARDS CAMERA ============ //
    void Update() {
        while (playersQueue.Count > 0) {
            this.opponent = (PlayerData) playersQueue.Dequeue();
            this.UpdatePlayerGameState();
        }
    }

    // ============ UPDATE PLAYER GAME STATE FROM SERVER ============ //
    public void UpdatePlayerGameState() {
        if (this.opponent.action == "grenade") {
            this.ThrowGrenade();
        }

        //update HP
        hpBar.fillAmount = this.opponent.hp/100f;
        hpText.text = this.opponent.hp.ToString();

        //update Shield
        shieldBar.fillAmount = this.opponent.shield_health/30f;
        shieldText.text = this.opponent.shield_health.ToString();
        if (shieldBar.fillAmount <= 0.1) shieldARObject.SetActive(false);
        else shieldARObject.SetActive(true); 
    }

    // ============= ACTIONS ================ //
    public void Shoot() {
        //Invoke player gets shot method
    }

    public void ThrowGrenade() {
        StartCoroutine(ThrowAnimation());
    }

    IEnumerator ThrowAnimation() {        
        //2s delay
        float time = 0;
        float duration = 2f;

        //instantiate positions
        GameObject grenade = Instantiate(grenadePrefab, imageTarget.transform.position, imageTarget.transform.rotation, imageTarget.transform);
        Vector3 startPos = grenade.transform.position;
        Vector3 targetPos = ARCamera.GetComponent<Transform>().transform.position;
        Vector3 movingPos = startPos;

        //start animation
        while (time < duration)
        {
            time += Time.deltaTime;
            movingPos = Vector3.Lerp(startPos, targetPos, time / duration);
            movingPos.y += grenadeParabolaCurve.Evaluate(time);
            grenade.transform.position = movingPos;
            yield return null;
        }

        //explode after 2s
        ParticleSystem grenadeExplosion = Instantiate(grenadeExplosionPrefab, grenade.transform.position, grenade.transform.rotation);
        Destroy(grenade);
        grenadeExplosion.Play();
    }

    public void Shield() {
        //Activate shield AR object
        shieldTimer = ShieldAnimation();
        StartCoroutine(shieldTimer);
    }

    IEnumerator ShieldAnimation() {
        ActivateShield();
        yield return new WaitForSecondsRealtime(10f);
        DeactivateShield();
    }

    public void ActivateShield() {
        shieldARObject.SetActive(true);
        shieldText.text = "30";
        shieldBar.fillAmount = 1;
    }

    public void DeactivateShield() {
        shieldARObject.SetActive(false);
        shieldText.text = "0";
        shieldBar.fillAmount = 0;
    }

    // =============== Player invoked actions ============== //
    // public void GunShotWithShield() {
    //     if (shieldBar.fillAmount <= 0.1) {
    //         StopCoroutine(shieldTimer);
    //         DeactivateShield();
    //     }
    // }

    // public void GunShotWithoutShield() {
    //     //No animation
    // }

    // public void GrenadeShotWithShield() {
    //     StopCoroutine(shieldTimer);
    //     DeactivateShield();
    // }

    // public void GrenadeShotWithoutShield() {
    //     //No animation
    // }

    // ============== Player Invoked ACTIONS ============== //
    //public void GunShot() {
        // if (isShieldActive) {
        //     shieldBar.fillAmount -= gunDmg * 10/3;
        //     shieldText.text = (int.Parse(shieldText.text) - 10).ToString();
        //     if (shieldBar.fillAmount <= 0.1) {
        //         StopCoroutine(shieldTimer);
        //         DeactivateShield();
        //     }
        // } else {
            // hpText.text = (int.Parse(hpText.text) - 10).ToString(); 
            // hpBar.fillAmount -= 0.1f;
        // }
    //}

    // public void GrenadeShot() {
    //     if (isShieldActive) {
    //         StopCoroutine(shieldTimer);
    //         DeactivateShield();
    //     } else {
    //         hpText.text = (int.Parse(hpText.text) - 30).ToString(); 
    //         hpBar.fillAmount -= grenadeDmg;
    //     }
    // }

    // ================= DEBUGGING ================= //
    public void Reset() {
        hpBar.fillAmount = 1;
        hpText.text = "100";
        shieldBar.fillAmount = 0;
        shieldText.text = "0";
    }
}
