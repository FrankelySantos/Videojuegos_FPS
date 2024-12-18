using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ImpactEffects
{
	[SerializeField] private string m_name;
	[SerializeField] private GameObject m_impactParticlePrefab;
	[SerializeField] private LayerMask m_impactLayerMask;

	public GameObject Prefab { get => m_impactParticlePrefab; }
	public LayerMask Layer { get => m_impactLayerMask; }
	public string Name { get => m_name; }
}

public class Weapon : WeaponBehavior
{
	[Header("Weapon Properties")]
	[SerializeField] private string m_weaponName;
	[SerializeField] private bool m_autoReload = true;
	[SerializeField] private LayerMask m_rayLayerMask;
	[Space]
	[SerializeField] private Transform m_fireTransform;
	[SerializeField] private int m_shotCount = 1;
	[SerializeField] private int m_magazineSize = 30;
	[SerializeField] private float m_shotImpulse = 400.0f;
	[SerializeField] private float m_FireRate = 600.0f;
	[SerializeField] private float m_spread = 0.1f;
	[SerializeField] private float m_fireDistance = 500.0f;
	[SerializeField] private float m_reloadDuration = 0.0f;

	[Header("Effects")]
	[SerializeField] private GameObject m_bulletPrefab;
	[SerializeField] private ParticleSystem[] m_muzzleEffects;

	[Header("Cheats")]
	[SerializeField] private bool m_infiniteAmmo = false;

	private GameObject m_ownerObject;
	private int m_currentAmmo = 0;
	private float m_lastFireTime = 0.0f;

	private bool m_isFiring = false;
	private bool m_canFire = true;
	private bool m_isReloading = false;

	public override GameObject GetOwnerObject() => m_ownerObject;
	public override int GetCurrentAmmo() => m_currentAmmo;
	public override int GetMagazineSize() => m_magazineSize;
	public override string GetWeaponName() => m_weaponName;
	public override bool GetIsFiring() => m_isFiring;
	public override bool GetIsReloading() => m_isReloading;
	public override Transform GetFireTransform() { return m_fireTransform; }

	public override void SetOwnerObject(GameObject owner)
	{
		m_ownerObject = owner;
	}

	protected override void Start()
	{
		m_currentAmmo = m_magazineSize;
	}

	protected override void Update()
	{
		if (m_isFiring && m_canFire) { Fire(); }
	}

	public override void StartFire()
	{
		m_isFiring = true;
	}

	public override void StopFire()
	{
		m_isFiring = false;
	}

	protected override void Fire()
	{
		if (Time.time - m_lastFireTime > 60.0f / m_FireRate)
		{
			m_lastFireTime = Time.time;

			if (m_currentAmmo > 0)
			{
				m_currentAmmo = m_infiniteAmmo ? m_magazineSize : m_currentAmmo - 1;

				PlayMuzzleParticles();

				Vector3 spreadValue = Random.insideUnitSphere * (m_spread);
				spreadValue.z = 0;
				spreadValue = m_fireTransform.TransformDirection(spreadValue);

				Quaternion rotation = Quaternion.LookRotation(Camera.main.transform.forward * 1000.0f + spreadValue - m_fireTransform.position);

				if (Physics.Raycast(new Ray(Camera.main.transform.position, Camera.main.transform.forward),
					out RaycastHit hit, m_fireDistance, m_rayLayerMask))
					rotation = Quaternion.LookRotation(hit.point + spreadValue - m_fireTransform.position);

				SpawnProjectile(rotation, spreadValue);
			}
			else
			{
				Reload();
			}
		}
	}

	private void SpawnProjectile(Quaternion rotation, Vector3 spread)
	{
		GameObject projectile = Instantiate(m_bulletPrefab, m_fireTransform.position, rotation);
		projectile.GetComponent<Projectile>().OwnerObject = m_ownerObject;
		projectile.transform.SetParent(null);
		projectile.GetComponent<Rigidbody>().velocity = (m_fireTransform.forward + spread) * m_shotImpulse;
	}

	private void PlayMuzzleParticles()
	{
		foreach (ParticleSystem p in m_muzzleEffects)
		{
			p.Play();
		}
	}

	public override void Reload()
	{
		StartCoroutine(ReloadDelay());
	}

	IEnumerator ReloadDelay()
	{
		m_canFire = false;
		m_isReloading = true;

		yield return new WaitForSeconds(m_reloadDuration);
		m_currentAmmo = m_magazineSize;

		m_isReloading = false;
		m_canFire = true;
	}
}
