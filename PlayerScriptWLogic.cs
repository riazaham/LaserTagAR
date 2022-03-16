using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using vu = Vuforia;

public class PlayerScriptWLogic : MonoBehaviour
{
    // =============== PLAYER DATA ================ //
    private PlayerData playerData = new PlayerData();

    // =============== ASSETS ============= //
    // health and shield assets
    public Image hpBar, shieldBar, shieldTimerProgressBar;
    public Text hpText, shieldText, shieldTimerText;
    public GameObject shieldTimerBg;

    // ammo assets
    public GameObject[] bullets, grenades, shields;
    public GameObject grenadePrefab;
    public Transform grenadeStartPos;

    // effect assets
    public ParticleSystem muzzleScreenFlash, grenadeScreenFlash, shieldScreenStream, grenadeExplosionPrefab;
    public AnimationCurve grenadeParabolaCurve;
    public Animation shotOutline, bloodyFrame, noMoreBullets, noMoreGrenades, noMoreShields, shieldIsActive, cannotReload;

    // sound assets
    public AudioClip shootSfx, shieldUpSfx, shieldDownSfx, reloadSfx, grenadeExplosionSfx, getHitSfx, shieldHitSfx; 
    private AudioSource audioSource;

    // background assets
    public GameObject shieldBg;

    // player and opponent location asset
    public Camera ARCamera;
    public GameObject imageTarget;

    // ============ FLAGS AND VARIABLES ============= //
    private bool isShieldActive = false;
    private int bulletsUsed, grenadesUsed, shieldsUsed = 0;
    private float gunDmg = 0.1f;
    private float grenadeDmg = 0.3f;
    private IEnumerator shieldTimer;
    private bool isOpponentVisible = false;

    // =============== Create singleton instance ============== //
    private static PlayerScriptWLogic _instance;
    public static PlayerScriptWLogic Player { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    // =================== Get data from server and update =================== //
    public void updatePlayerData(PlayerData playerData) {
        this.playerData = playerData;
    }

    // =============== Opponent detection method for vuforia script =============== //
    public void setOpponentVisible(bool isVisible) {
        isOpponentVisible = isVisible;
    }

    // ============= ACTIONS ================ //
    public void Shoot() {
        if (bulletsUsed == 6) noMoreBullets.Play();
        else {
            bullets[bulletsUsed++].SetActive(false);
            muzzleScreenFlash.Play();
            audioSource.clip = shootSfx;
            audioSource.Play();
            StartCoroutine(CameraFlash());

            if (isOpponentVisible) {
                OpponentScript.Opponent.GunShot();
                shotOutline.Play();
            }
        }
    }

    IEnumerator CameraFlash() {
        vu.VuforiaBehaviour.Instance.CameraDevice.SetFlash(true);
        yield return new WaitForSecondsRealtime(0.1f);
        vu.VuforiaBehaviour.Instance.CameraDevice.SetFlash(false);
    }

    public void ThrowGrenade() {
        if (grenadesUsed == 3) noMoreGrenades.Play();
        else {
            grenades[grenadesUsed++].SetActive(false);
            StartCoroutine(ThrowAnimation());
        } 
    }

    IEnumerator ThrowAnimation() {        
        //2s delay
        float time = 0;
        float duration = 2f;

        //instantiate positions
        GameObject grenade = Instantiate(grenadePrefab, grenadeStartPos.position, grenadeStartPos.rotation, ARCamera.transform);
        Vector3 startPos = grenade.transform.position;
        Vector3 targetPos = imageTarget.GetComponent<Transform>().transform.position;
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
        audioSource.clip = grenadeExplosionSfx;
        audioSource.Play();

        if (isOpponentVisible) {
            OpponentScript.Opponent.GrenadeShot();
            shotOutline.Play();
        }
    }

    public void Shield() {
        if (shieldsUsed == 2) noMoreShields.Play();
        else if (isShieldActive) shieldIsActive.Play();
        else {
            shields[shieldsUsed++].SetActive(false);
            shieldTimer = ShieldAnimation();
            StartCoroutine(shieldTimer);
        }
    }

    IEnumerator ShieldAnimation() {
        ActivateShield();
        for (int timer = 10; timer >= 0; timer--) {
            shieldTimerText.text = timer.ToString();
            yield return new WaitForSecondsRealtime(1f);
            shieldTimerProgressBar.fillAmount -= 0.1f;
        }
        DeactivateShield();
    }

    void ActivateShield() {
        //Set flags
        isShieldActive = true;
        shieldTimerBg.SetActive(true);
        shieldBg.SetActive(true);

        //Set UI
        shieldTimerText.text = "10";
        shieldText.text = "30";
        shieldBar.fillAmount = 1;
        shieldTimerProgressBar.fillAmount = 1;
        
        //Play animation and sound
        shieldScreenStream.Play();
        audioSource.clip = shieldUpSfx;
        audioSource.Play();
    }

    void DeactivateShield() {
        //Reset flags
        isShieldActive = false;
        shieldTimerBg.SetActive(false);
        shieldBg.SetActive(false);

        //Reset UI
        shieldTimerText.text = "";
        shieldText.text = "0";
        shieldBar.fillAmount = 0;
        shieldTimerProgressBar.fillAmount = 1;

        //Stop animation
        shieldScreenStream.Stop();
        audioSource.clip = shieldDownSfx;
        audioSource.Play();
    }

    public void Reload() {
        if (bulletsUsed == 6) {
            for (int ammo = 0; ammo < bulletsUsed; ammo++) {
                bullets[ammo].SetActive(true);
            }
            bulletsUsed = 0;
            audioSource.clip = reloadSfx;
            audioSource.Play();
        } else cannotReload.Play();
    }

    // ============== Opponent Invoked ACTIONS ============== //
    public void GunShot() {
        if (isShieldActive) {
            shieldBar.fillAmount -= gunDmg * 10/3;
            shieldText.text = (int.Parse(shieldText.text) - 10).ToString();
            audioSource.clip = shieldHitSfx;
            audioSource.Play();
            if (shieldBar.fillAmount <= 0.1) {
                StopCoroutine(shieldTimer);
                DeactivateShield();
            }
        } else {
            hpText.text = (int.Parse(hpText.text) - 10).ToString(); 
            hpBar.fillAmount -= gunDmg;
            bloodyFrame.Play();
            audioSource.clip = getHitSfx;
            audioSource.Play();
        }
    }

    public void GrenadeShot() {
        if (isShieldActive) {
            StopCoroutine(shieldTimer);
            audioSource.clip = shieldHitSfx;
            audioSource.Play();
            DeactivateShield();
        } else {
            hpText.text = (int.Parse(hpText.text) - 30).ToString(); 
            hpBar.fillAmount -= grenadeDmg;
            grenadeScreenFlash.Play();
            bloodyFrame.Play();
        }
        audioSource.clip = grenadeExplosionSfx;
        audioSource.Play();
    }

    // ================= DEBUGGING ================= //
    public void Reset() {
        hpBar.fillAmount = 1;
        hpText.text = "100";
        shieldBar.fillAmount = 0;
        shieldText.text = "0";
        for (int ammo = 0; ammo < 6; ammo++) {
            bullets[ammo].SetActive(true);
        }
        for (int ammo = 0; ammo < 3; ammo++) {
            grenades[ammo].SetActive(true);
        }
        for (int ammo = 0; ammo < 2; ammo++) {
            shields[ammo].SetActive(true);
        }
        isShieldActive = false;
        bulletsUsed = 0;
        grenadesUsed = 0;
        shieldsUsed = 0;
    }
}
