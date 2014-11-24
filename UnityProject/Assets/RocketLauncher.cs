﻿using UnityEngine;
using System.Collections;

public class RocketLauncher : MonoBehaviour 
{
	public Transform Owner;

	public float RotateSpeed = 1.0f;
	private string rocketPool = "Robot1_Rocket";
	public Transform Rocket_Pos1;
	public Transform target;

	public float MaxAngle = 20;
	public float angle = 0;


	// Update is called once per frame
	void Update()
	{
		float delta = Time.deltaTime / 0.016f;

		Vector3 diff = Rocket_Pos1.localPosition;
		Vector3 currentDirection = transform.forward;
		Vector3 lookDirection = Owner.forward;
		if (target)
		{
			lookDirection = (target.position + diff + Vector3.up) - transform.position;
			angle = Vector3.Angle(Owner.forward, lookDirection);
			if (angle > MaxAngle)
				lookDirection = Owner.forward;
		}

		

		transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(lookDirection), Mathf.Min(delta * RotateSpeed, 1f));
	}

	public bool Shoot()
	{
		if (angle > MaxAngle)
			return false;

		GameObject go = GameObjectPool.Instance.Spawn(rocketPool, Rocket_Pos1.position, Rocket_Pos1.rotation);
		Rocket rocket = go.GetComponent<Rocket>();
		if (rocket)
		{
			rocket.Owner = this.Owner;
		}
		return true;
	}
}
