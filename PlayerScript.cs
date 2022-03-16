using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using vu = Vuforia;

public class PlayerScript : MonoBehaviour
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

    private IEnumerator shieldTimer;

    // =============== Create singleton instance ============== //
    private static PlayerScript _instance;
    public static PlayerScript Player { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;
        audioSource = GetComponent<AudioSource>();
    }

    // =============== UPDATE OPPONENT DETECTION ON SERVER =============== //
    public void setOpponentVisible(bool isVisible) {
        TCPClient tcpClient = new TCPClient();
        tcpClient.SendMessage(isVisible);
    }

    // ============ UPDATE PLAYER GAME STATE FROM SERVER ============ //
    public void UpdatePlayerGameState(PlayerData playerData) {
        this.playerData = playerData;
        
        //update HP
        hpBar.fillAmount = this.playerData.getPlayerHP()/100;
        hpText.text = this.playerData.getPlayerHP().ToString();

        //update Shield
        shieldBar.fillAmount = this.playerData.getPlayerSP()/30;
        hpText.text = this.playerData.getPlayerSP().ToString();

        //update gun ammo (retrieving ammo used)
        for (int ammo = this.playerData.getPlayerGunAmmo(); ammo < bullets.Length; ammo++) {
            bullets[ammo].SetActive(false);
        }

        //update grenade ammo (retrieving ammo used)
        for (int ammo = this.playerData.getPlayerGrenadeAmmo(); ammo < grenades.Length; ammo++) {
            grenades[ammo].SetActive(false);
        }

        //update shield ammo (retrieving ammo used)
        for (int ammo = this.playerData.getPlayerShieldAmmo(); ammo < shields.Length; ammo++) {
            shields[ammo].SetActive(false);
        }
    }

    // ============= ACTIONS ================ //
    public void Shoot() {
        //bullets decreased in update player game state
        
        //play shot animation and sound
        muzzleScreenFlash.Play();
        audioSource.clip = shootSfx;
        audioSource.Play();

        //flash camera
        StartCoroutine(CameraFlash());
    }

    IEnumerator CameraFlash() {
        vu.VuforiaBehaviour.Instance.CameraDevice.SetFlash(true);
        yield return new WaitForSecondsRealtime(0.1f);
        vu.VuforiaBehaviour.Instance.CameraDevice.SetFlash(false);
    }

    public void ThrowGrenade() {
        //grenades decreased in update player game state

        //start throw animation
        StartCoroutine(ThrowAnimation());
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

        //play grenade animation and sound
        grenadeExplosion.Play();
        audioSource.clip = grenadeExplosionSfx;
        audioSource.Play();
    }

    public void Shield() {
        //shields decreased in update player game state

        //start shield animation
        shieldTimer = ShieldAnimation();
        StartCoroutine(shieldTimer);
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
        //bullets refilled in update player game state

        //play reload sound
        audioSource.clip = reloadSfx;
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
    }
}
