using System;
using System.Collections;
using UnityEngine;

public class EnemyWalker : Enemy
{
    private Rigidbody rb;
    private Transform playerTf;
    [SerializeField] Animator anim;
    [SerializeField] Animator animLod;

    private bool isRotating = false;
    private float rotationProgress = -1;
    private bool isRotateOnCooldown = false;
    private Quaternion targetRotation;
    private Quaternion initialRotation;
    [Header("Idle Rotation")]
    [SerializeField] float idleRotatePause = 0.7f;
    [SerializeField] float idleRotateSpeed = 2f;

    private new void Awake() {
        base.Awake();
        rb = GetComponent<Rigidbody>();
        playerTf = GameObject.Find("Player").GetComponent<Transform>();
    }

    private new void Start() {
        base.Start();
    }

    void Update() {
        if(!gameManager.gameOver && hp > 0) {
            float distance = (playerTf.position - transform.position).magnitude;

            if (distance > stats.detectRange) {
                anim.SetBool("isIdle", true);
                animLod.SetBool("isIdle", true);
                Idle();
            }
            else if (distance <= stats.detectRange && distance > stats.attackRange) {
                anim.SetBool("isIdle", false);
                animLod.SetBool("isIdle", false);
                TrackPlayer();
            }
            else {
                anim.SetBool("isIdle", false);
                animLod.SetBool("isIdle", false);
                Attack();
            }
        }
    }

    protected override void Attack() {
        TrackPlayer();
    }

    protected override void Idle() {
        if (!isRotateOnCooldown) {
            if (isRotating) {
                if (rotationProgress < 1 && rotationProgress >= 0) {
                    rotationProgress += Time.deltaTime * idleRotateSpeed;
                    transform.rotation = Quaternion.Lerp(initialRotation, targetRotation, rotationProgress);
                }
                else {
                    isRotating = false;
                    rotationProgress = -1;
                    StartCoroutine(nameof(IdleRotateCooldown));
                }
            }else {
                initialRotation = transform.rotation;
                targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, UnityEngine.Random.Range(0f, 360f), transform.rotation.eulerAngles.z);
                isRotating = true;
                rotationProgress = 0;
            }
        }
    }

    private IEnumerator IdleRotateCooldown() {
        isRotateOnCooldown = true;

        yield return new WaitForSeconds(idleRotatePause);

        isRotateOnCooldown = false;
    }

    protected override void TrackPlayer() {
        Vector3 moveDirection = playerTf.position - transform.position;
        moveDirection.y = 0;

        Vector3 lookDirection = playerTf.position;  
        lookDirection = new Vector3(lookDirection.x, transform.position.y, lookDirection.z);

        rb.AddForce(moveDirection.normalized * stats.speed);
        transform.LookAt(lookDirection);
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Player")) {
            anim.SetBool("isAttacking", true);
            InvokeRepeating(nameof(PerformAttack), 0, stats.collideDmgCooldown);
        }
    }

    private void OnCollisionExit(Collision collision) {
        if (collision.gameObject.CompareTag("Player")) {
            anim.SetBool("isAttacking", false);
            CancelInvoke(nameof(PerformAttack));
        }
    }

    private void PerformAttack() {    
        gameManager.DamagePlayer(stats.collideDamage);
    }

    protected override void OnDeath() {
        healthBar.gameObject.SetActive(false);
        Destroy(gameObject.GetComponent<Rigidbody>());
        gameObject.GetComponent<CapsuleCollider>().enabled = false;
        anim.SetTrigger("death");
        animLod.SetTrigger("death");
        gameManager.EnemyKilled();
        Invoke(nameof(Die), gameManager.settings.deleteCorpsesIn);
    }
}
