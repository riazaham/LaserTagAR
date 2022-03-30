using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using vu = Vuforia;

public class PlayerScript : MonoBehaviour
{
    //Players buffer
    public Queue playersQueue = new Queue();
    PlayerData p1;

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

    // AR UI Canvas
    public Canvas ARUICanvas;
    public GameObject gameOver;

    // ============ FLAGS AND VARIABLES ============= //

    private IEnumerator shieldTimer;
    private bool isShieldActive;
    private bool isOpponentVisible = false;
    private bool isBulletsEmpty = false;
    private bool isGrenadesEmpty = false;
    private bool isShieldsEmpty = false;


    // =============== Create singleton instance ============== //
    private static PlayerScript _instance;
    public static PlayerScript Player { get { return _instance; } }
    private void Awake()
    {
        if (_instance != null && _instance != this) Destroy(this.gameObject);
        else _instance = this;
        audioSource = GetComponent<AudioSource>();

        string state = "{\"p1\": {\"hp\": 70, \"action\": \"none\", \"bullets\": 3, \"grenades\": 1, \"shield_time\": 0, \"shield_health\": 20, \"num_deaths\": 0, \"num_shield\": 1}, \"p2\": {\"hp\": 90, \"action\": \"none\", \"bullets\": 6, \"grenades\": 2, \"shield_time\": 0, \"shield_health\": 30, \"num_deaths\": 0, \"num_shield\": 3}}";
        Players players = JsonUtility.FromJson<Players>(state);
        this.playersQueue.Enqueue(players.p1);
    }

    // =============== UPDATE OPPONENT DETECTION ON SERVER =============== //
    public void setOpponentVisible(bool isVisible) {
        this.isOpponentVisible = isVisible;
        TCPClient.tCPClient.SendMessage(isVisible);
    }

    // Enqueue and dequeue
    void Update() {
        while (this.playersQueue.Count > 0) {
            this.p1 = (PlayerData) playersQueue.Dequeue();
            this.UpdatePlayerGameState();
        }
    }

    // ============ UPDATE PLAYER GAME STATE FROM SERVER ============ //
    public void UpdatePlayerGameState() {
        if (this.p1.action == "shoot") {
            this.Shoot();
        } else if (this.p1.action == "shield") {
            this.Shield();
        } else if (this.p1.action == "reload") {
            this.Reload();
        } else if (this.p1.action == "grenade") {
            this.ThrowGrenade();
        } else if (this.p1.action == "logout") {
            gameOver.SetActive(true);
        }

        //update HP
        hpBar.fillAmount = this.p1.hp/100f;
        hpText.text = this.p1.hp.ToString();

        //update Shield
        shieldBar.fillAmount = this.p1.shield_health/30f;
        shieldText.text = this.p1.shield_health.ToString();

        //update gun ammo
        for (int ammo = 0; ammo < 6; ammo++) {
            if (ammo < this.p1.bullets) bullets[ammo].SetActive(true);
            else bullets[ammo].SetActive(false);
        }

        //update grenade ammo
        for (int ammo = 0; ammo < 2; ammo++) {
            if (ammo < this.p1.grenades) grenades[ammo].SetActive(true);
            else grenades[ammo].SetActive(false);
        }

        //update shield ammo
        for (int ammo = 0; ammo < 3; ammo++) {
            if (ammo < this.p1.num_shield) shields[ammo].SetActive(true);
            else shields[ammo].SetActive(false);
        }
    }

    // ============= ACTIONS ================ //
    public void Shoot() {
        //bullets decreased in update player game state
        if (this.p1.bullets >= 0 && !this.isBulletsEmpty) {        
            //play shot animation and sound
            muzzleScreenFlash.Play();
            audioSource.clip = shootSfx;
            audioSource.Play();

            //flash camera
            StartCoroutine(CameraFlash());
            if (this.p1.bullets == 0) this.isBulletsEmpty = true;
        } else {
            noMoreBullets.Play();
        }
    }

    IEnumerator CameraFlash() {
        vu.VuforiaBehaviour.Instance.CameraDevice.SetFlash(true);
        yield return new WaitForSecondsRealtime(0.1f);
        vu.VuforiaBehaviour.Instance.CameraDevice.SetFlash(false);
    }

    public void ThrowGrenade() {
        if (this.p1.grenades >= 0 && !this.isGrenadesEmpty) {
            //start throw animation
            StartCoroutine(ThrowAnimation());

            if (this.p1.grenades == 0) this.isGrenadesEmpty = true;
        } else {
            noMoreGrenades.Play();
        }
    }

    IEnumerator ThrowAnimation() {    
        //2s delay
        float time = 0;
        float duration = 2f;

        //instantiate positions
        GameObject grenade = Instantiate(grenadePrefab, grenadeStartPos.position, grenadeStartPos.rotation, ARCamera.transform);
        Vector3 startPos = grenade.transform.position;
        Vector3 targetPos;
        if (isOpponentVisible) {
            Vector3 pos = ARUICanvas.GetComponent<Transform>().transform.position;
            targetPos = new Vector3(pos.x, pos.y, pos.z - 0.1f);

        } else {
            targetPos = new Vector3(startPos.x, startPos.y, startPos.z + 2f);
        }
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

        Transform grenadePos = grenade.transform;
        Destroy(grenade);
        if (isOpponentVisible) {
            //explode after 2s
            ParticleSystem grenadeExplosion = Instantiate(grenadeExplosionPrefab, grenadePos.position, grenadePos.rotation);

            //play grenade animation and sound
            grenadeExplosion.Play();
            audioSource.clip = grenadeExplosionSfx;
            audioSource.Play();
        }
    }

    public void Shield() {
        if (this.isShieldActive) {
            shieldIsActive.Play();
        } else if (this.p1.num_shield >= 0 && !this.isShieldsEmpty) {
            //start shield animation
            shieldTimer = ShieldAnimation();
            isShieldActive = true;
            StartCoroutine(shieldTimer);

            if (this.p1.num_shield == 0) this.isShieldsEmpty = true;
        } else {
            noMoreShields.Play();
        }
    }

    IEnumerator ShieldAnimation() {
        ActivateShield();
        for (int timer = 10; timer >= 1; timer--) {
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
        if (this.p1.bullets == 6 && this.isBulletsEmpty) {
            //play reload sound
            audioSource.clip = reloadSfx;
            audioSource.Play();
            this.isBulletsEmpty = false;   
        } else {
            cannotReload.Play();
        }
    }

    // =============== Opponent invoked actions ============== //
    // public void GetShot() {
    //     if (isShieldActive) {
    //         audioSource.clip = shieldHitSfx;
    //         audioSource.Play();
    //         if (this.p1.shield_health == 0) {
    //             StopCoroutine(shieldTimer);
    //             DeactivateShield();
    //         }
    //     } else {
    //         bloodyFrame.Play();
    //         audioSource.clip = getHitSfx;
    //         audioSource.Play();
    //     }
    // }

    // public void GrenadeShot() {
    //     if (isShieldActive) {
    //         StopCoroutine(shieldTimer);
    //         audioSource.clip = shieldHitSfx;
    //         audioSource.Play();
    //         DeactivateShield();
    //     } else {
    //         grenadeScreenFlash.Play();
    //         bloodyFrame.Play();
    //         audioSource.clip = grenadeExplosionSfx;
    //         audioSource.Play();
    //     }
    // }

    // ================= DEBUGGING ================= //
    public void Reset() {
        hpBar.fillAmount = 1;
        hpText.text = "100";
        shieldBar.fillAmount = 0;
        shieldText.text = "0";
        for (int ammo = 0; ammo < 6; ammo++) {
            bullets[ammo].SetActive(true);
        }
        for (int ammo = 0; ammo < 2; ammo++) {
            grenades[ammo].SetActive(true);
        }
        for (int ammo = 0; ammo < 3; ammo++) {
            shields[ammo].SetActive(true);
        }
    }
}
