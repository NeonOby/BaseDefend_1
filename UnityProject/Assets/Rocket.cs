﻿using UnityEngine;
using System.Collections;

public class Rocket : MonoBehaviour {

	private string poolName = "";
	public Transform Owner;

	public Timer lifeTimer;

    public ParticleSystem particles;
    public Timer DespawnTimer;
	public float ForceForward = 2.0f;
	[Range(0f, 1f)]
	public float Damping = 0.5f;

    public float ExplosionRange = 2.0f;
    public float DefaultExplosionDamage = 10f;
    public float ExplosionDamageForDifficulty = 100f;
    public float ExplosionDamage
    {
        get
        {
            return DefaultExplosionDamage + ExplosionDamageForDifficulty * Game.DifficultyLevel;
        }
    }
	public float ExplosionForce = 100f;

    public Renderer rocketRenderer;

    private bool IsDead = false;
    public LayerMask ExplosionMask;

    public Collider Collider;

    public void Awake()
    {
        Data.Instance.Register(this);
    }

	// Use this for initialization
	void Reset () 
	{
		rigidbody.velocity = Vector3.zero;
		lifeTimer.Reset();
        IsDead = false;
        Collider.enabled = true;
        particles.enableEmission = true;
        rocketRenderer.enabled = true;
	}

	void SetPoolName(string value)
	{
		poolName = value;
	}

    public void OnMessage_EnemyFixedDeltaTimeChanged(float difference)
    {
        rigidbody.velocity -= rigidbody.velocity * difference;
        rigidbody.angularVelocity -= rigidbody.angularVelocity * difference;

        if (Game.EnemyFixedDelta == 0)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

	void Update()
	{
		if (!IsDead && lifeTimer.Update())
			Explode();
        if (IsDead && DespawnTimer.Update())
            GameObjectPool.Instance.Despawn(poolName, gameObject);
	}

	// Update is called once per frame
	void FixedUpdate()
	{
        if (IsDead)
            return;

		float delta = Game.EnemyFixedDelta;
		rigidbody.velocity += transform.forward * ForceForward * delta;
		rigidbody.velocity -= rigidbody.velocity * Damping * delta;
	}


	public void Explode()
	{
        if (IsDead)
            return;

        if (ExplosionRange > 0)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, ExplosionRange, ExplosionMask);
            foreach (var item in hits)
            {
                TryDamage(item);
            }
        }
        IsDead = true;
        Collider.enabled = false;
        rigidbody.velocity = Vector3.zero;
        particles.enableEmission = false;
        rocketRenderer.enabled = false;
        DespawnTimer.Reset();
	}

	public void TryDamage(Collider item)
	{
        float procentage = 1f;
        if (ExplosionRange > 0)
            procentage = Vector3.Distance(item.transform.position, transform.position) / ExplosionRange;

		Rigidbody rig = item.rigidbody;
		if (rig)
		{
			rig.AddForce((item.transform.position - transform.position).normalized * procentage * ExplosionForce);
		}
		item.SendMessage("Damage", new vp_DamageInfo(procentage * ExplosionDamage, Owner), SendMessageOptions.DontRequireReceiver);
	}

	void OnTriggerEnter(Collider other)
	{
        if (IsDead)
            return;
		TryDamage(other);
		Explode();
	}
}
