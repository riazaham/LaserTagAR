using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpponentScript : MonoBehaviour
{
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


    // ============ FLAGS AND VARIABLES ============= //
    private bool isShieldActive = false;
    private float gunDmg = 0.1f;
    private float grenadeDmg = 0.3f;
    private IEnumerator shieldTimer;

    // =============== Create singleton instance ============== //
    private static OpponentScript _instance;
    public static OpponentScript Opponent { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;
    }

    // ============= ACTIONS ================ //
    public void Shoot() {
        //Invoke player gets shot method
        //PlayerScript.Player.GunShot();
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

        //Invoke player gets hit by grenade method
        //PlayerScript.Player.GrenadeShot();
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
        isShieldActive = true;
        shieldARObject.SetActive(true);
        shieldText.text = "30";
        shieldBar.fillAmount = 1;
    }

    public void DeactivateShield() {
        isShieldActive = false;
        shieldARObject.SetActive(false);
        shieldText.text = "0";
        shieldBar.fillAmount = 0;
    }

    // ============== Player Invoked ACTIONS ============== //
    public void GunShot() {
        if (isShieldActive) {
            shieldBar.fillAmount -= gunDmg * 10/3;
            shieldText.text = (int.Parse(shieldText.text) - 10).ToString();
            if (shieldBar.fillAmount <= 0.1) {
                StopCoroutine(shieldTimer);
                DeactivateShield();
            }
        } else {
            hpText.text = (int.Parse(hpText.text) - 10).ToString(); 
            hpBar.fillAmount -= gunDmg;
        }
    }

    public void GrenadeShot() {
        if (isShieldActive) {
            StopCoroutine(shieldTimer);
            DeactivateShield();
        } else {
            hpText.text = (int.Parse(hpText.text) - 30).ToString(); 
            hpBar.fillAmount -= grenadeDmg;
        }
    }

    // ================= DEBUGGING ================= //
    public void Reset() {
        hpBar.fillAmount = 1;
        hpText.text = "100";
        shieldBar.fillAmount = 0;
        shieldText.text = "0";
        isShieldActive = false;
    }
}
