using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Cinemachine;
using TMPro;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Player Stats")]
    [SerializeField] int playerHp = 100;
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float rotationSpeed;
    [SerializeField] TextMeshProUGUI playerHpText;

    private Animator playerAnim;

    [Header("Shoot Settings")]
    [SerializeField] float shootTime;
    [SerializeField] int bulletForce = 10;
    [SerializeField] private Transform spawnObjectPrefab;
    [SerializeField] private Transform bulletSpawn;
    [SerializeField] private Animator gunAnim;

    private bool canShoot;
    private Transform spawnedObjectTransform;

    [Header("Camera")]
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform cameraTransform;


    private void Start()
    {
        gunAnim = GetComponent<Animator>();
        playerAnim = GetComponent<Animator>();  
        canShoot = true;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            virtualCamera.Priority = 1;
        } 
        else
        {
            virtualCamera.Priority = 0;
        }
    }

    private void FixedUpdate()
    {
        //PlayerRotation
        Quaternion rotation = Quaternion.Euler(0, cameraTransform.eulerAngles.y, 0);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

        playerHpText.text = "HP " + playerHp.ToString("00");
    }

    void Update()
    {
        if (!IsOwner) return;

        //PlayerMovement
        Vector3 direction = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) direction.z = 1f;
        if (Input.GetKey(KeyCode.S)) direction.z = -1f;
        if (Input.GetKey(KeyCode.A)) direction.x = -1f;
        if (Input.GetKey(KeyCode.D)) direction.x = 1f;

        transform.Translate(direction * moveSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))
        {
            playerAnim.SetTrigger("Run");
            gunAnim.SetTrigger("Run");
        } 
        else
        {
            playerAnim.SetTrigger("Idle");
            gunAnim.SetTrigger("Idle");
        }

        //PlayerShooting
        if (Input.GetMouseButton(0) && canShoot)
        {
            StartCoroutine(shootCountdown());
            TestServerRpc();
        }
    }

    IEnumerator shootCountdown()
    {
        canShoot = false;
        yield return new WaitForSeconds(shootTime);
        canShoot = true;
    }

    void Shoot()
    {
        spawnedObjectTransform = Instantiate(spawnObjectPrefab, bulletSpawn.position, bulletSpawn.rotation);
        spawnedObjectTransform.GetComponent<NetworkObject>().Spawn(true);

        Rigidbody bulletRb = spawnedObjectTransform.GetComponent<Rigidbody>();
        bulletRb.AddRelativeForce((Vector3.forward * bulletForce), ForceMode.Impulse);
    }

    [ServerRpc]
    private void TestServerRpc()
    {
        Shoot();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bullet"))
        {
            playerHp = playerHp - 10;
            collision.gameObject.GetComponent<NetworkObject>().Despawn();
        }
    }
}
