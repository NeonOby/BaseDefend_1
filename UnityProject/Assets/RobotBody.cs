﻿using UnityEngine;
using System.Collections;

[System.Serializable]
public class Leg
{
    private const float PI = 3.1415f;
    public string Name;

    public RobotBody Owner;
    public Transform Root;
    public Transform Foot;
    public Transform FootIK;
    public float MaxFeetDistance = 1.2f;
    public float TransitionTime = 1.0f;
    public float TransitionYDiff = 0.5f;
    public float TransitionDamping = 1.0f;

    public Vector4 moveRestriction;

    //DEBUG
    public float CurrentDistance = 0f;
    public bool needsNewPosition = false;

    private float TransitionTimer;
    private Vector3 LastPosition;
    private Vector3 WantedPosition;

    
    private float lastY = 0f;

    public void Start()
    {
        LastPosition = Foot.position;
        WantedPosition = FootIK.position;
        TransitionTimer = 0f;
        lastY = LastPosition.y;
        moving = true;
    }

    private bool NeedsNewPosition()
    {
        Vector3 min = new Vector3(moveRestriction.x, 0, moveRestriction.z);
        Vector3 max = new Vector3(moveRestriction.y, 0, moveRestriction.w);

        Vector3 position = Owner.body.InverseTransformPoint(FootIK.position);

        if (position.x < min.x || position.x > max.x)
            return true;
        if (position.z < min.z || position.z > max.z)
            return true;

        return false;
    }

    private bool GetGround(Vector3 position, out Vector3 hitPoint)
    {
        RaycastHit hit;
        Vector3 origin = position;
        origin.y = Owner.body.position.y + Owner.SlopeStairHeight;
        if (Physics.Raycast(origin, Vector3.down, out hit, MaxFeetDistance + Owner.SlopeStairHeight, Owner.FootWalkLayerMask))
        {
            hitPoint = hit.point;
            return true;
        }
        hitPoint = position;
        return false;
    }

    public void DebugRestrictionArea()
    {
        Vector3 min = new Vector3(moveRestriction.x, 0, moveRestriction.z);
        Vector3 max = new Vector3(moveRestriction.y, 0, moveRestriction.w);
        Vector3 pos1 = min;
        Vector3 pos2 = new Vector3(min.x, min.y, max.z);
        Vector3 pos3 = max;
        Vector3 pos4 = new Vector3(max.x, min.y, min.z);

        pos1 = Owner.body.TransformPoint(pos1);
        pos2 = Owner.body.TransformPoint(pos2);
        pos3 = Owner.body.TransformPoint(pos3);
        pos4 = Owner.body.TransformPoint(pos4);
        
        pos1.y -= Owner.YDistanceToFeet;
        pos2.y -= Owner.YDistanceToFeet;
        pos3.y -= Owner.YDistanceToFeet;
        pos4.y -= Owner.YDistanceToFeet;

        Debug.DrawLine(pos1, pos2, Color.red);
        Debug.DrawLine(pos2, pos3, Color.red);
        Debug.DrawLine(pos3, pos4, Color.red);
        Debug.DrawLine(pos4, pos1, Color.red);

        Vector3 centerOfFootPlacement = Vector3.zero;
        centerOfFootPlacement.x = min.x + (max.x - min.x)/2;
        centerOfFootPlacement.z = min.z + (max.z - min.z)/2;
        centerOfFootPlacement = Owner.body.TransformPoint(centerOfFootPlacement);
        centerOfFootPlacement.y -= Owner.YDistanceToFeet;

        Debug.DrawRay(centerOfFootPlacement, Vector3.up, Color.green);
    }

    public float dotToWalkDirection;
    public float procentage = 0f;

    public bool moving = true;

    public void Update()
    {
        //Stick on ground
        GetGround(WantedPosition, out WantedPosition);

        //Animate to next wanted position
        TransitionTimer = Mathf.Min(TransitionTimer + Time.deltaTime, TransitionTime);
        procentage = (TransitionTimer / TransitionTime);
        float sinVal = Mathf.Sin(procentage * PI);

        Vector3 wantedPos = procentage < 0.5f ? LastPosition : WantedPosition;
        wantedPos.y += sinVal * TransitionYDiff;

        Vector3 diff = wantedPos - FootIK.position;
        diff = diff.normalized * Mathf.Min(TransitionDamping, diff.magnitude);
        FootIK.position += diff;

        needsNewPosition = NeedsNewPosition();

        if (moving && OnGround)
        {
            Owner.FootStepOnGround();
            moving = false;
        }

        //Check if body moves and try to find new position for footIK
        if (needsNewPosition)
        {
            //We stay on the floor and user cant lose foot, return
            if (!moving)
            {
                if(Owner.CanLoseFoot)
                    Owner.LoseFoot();
                else
                    return;
            }

            

            Vector3 newPos = Vector3.zero;
            Vector3 DiffToCenter = Vector3.zero;
            Vector3 centerOfFootPlacement = Vector3.zero;

            Vector3 min = new Vector3(moveRestriction.x, 0, moveRestriction.z);
            Vector3 max = new Vector3(moveRestriction.y, 0, moveRestriction.w);
            Vector3 pos1 = min;
            Vector3 pos2 = new Vector3(min.x, min.y, max.z);
            Vector3 pos3 = max;
            Vector3 pos4 = new Vector3(max.x, min.y, min.z);

            pos1 = Owner.body.TransformPoint(pos1);
            pos2 = Owner.body.TransformPoint(pos2);
            pos3 = Owner.body.TransformPoint(pos3);
            pos4 = Owner.body.TransformPoint(pos4);
            
            centerOfFootPlacement.x = min.x + (max.x - min.x) / 2;
            centerOfFootPlacement.z = min.z + (max.z - min.z) / 2;

            Vector3 moveDirection = Owner.body.InverseTransformDirection(Owner.body.rigidbody.velocity);
            if (moveDirection.magnitude == 0f)
            {
                moveDirection = (Owner.body.position + centerOfFootPlacement) - FootIK.position;
            }
            moveDirection.y = 0f;
            moveDirection.Normalize();

            newPos = centerOfFootPlacement + moveDirection * 100f;

            newPos.x = Mathf.Clamp(newPos.x, min.x, max.x);
            newPos.z = Mathf.Clamp(newPos.z, min.z, max.z);
            newPos = Owner.body.TransformPoint(newPos);
            newPos.y = 0f;

            if (!GetGround(newPos, out newPos))
            {
                centerOfFootPlacement = Owner.body.TransformPoint(centerOfFootPlacement);
                centerOfFootPlacement.y = Owner.body.position.y - Owner.YDistanceToFeet;
                GetGround(centerOfFootPlacement, out newPos);
            }

            LastPosition = WantedPosition;
            WantedPosition = newPos;
            TransitionTimer = 0f;
            moving = true;
        }
    }

    public bool OnGround
    {
        get
        {
            return procentage >= 1f && Vector3.Distance(FootIK.position, WantedPosition) < 0.01f;
        }
    }
}

[ExecuteInEditMode]
public class RobotBody : MonoBehaviour 
{
    public LayerMask FootWalkLayerMask;
    public Leg[] legs = {};

    public int feetOnGround = 0;

	// Use this for initialization
	void Start () 
    {
        feetOnGround = 0;
        foreach (var item in legs)
        {
            item.Owner = this;
            item.Start();
        }
	}

    public float FootLoseTime = 0.15f;
    public float FootOnGroundTime = 0.1f;

    private float FootLoseTimer = 0f;
    private float FootOnGroundTimer = 0f;

    public float SlopeStairHeight = 1.0f;

    public bool CanLoseFoot
    {
        get
        {
            return FootLoseTimer >= FootLoseTime && feetOnGround >= 0;
        }
    }
    public void LoseFoot()
    {
        feetOnGround--;
        FootLoseTimer = 0f;
    }
    public void FootStepOnGround()
    {
        feetOnGround++;
        FootOnGroundTimer = 0f;
    }

    public bool Debug = true;
    public bool RunInEditor = true;

    public float MoveSpeed = 2.0f;
    public float RotateSpeed = 2.0f;

    public Transform body;

    [Range(0f, 1f)]
    public float VelocityDamping = 0.2f;
    [Range(0f, 1.0f)]
    public float RotateVelocityDamping = 0.2f;

    public float GroundDistance = 0.75f;
    [Range(0f, 5f)]
    public float HeightSpring = 5.0f;
    [Range(0f, 1f)]
    public float HeightDamping = 1.0f;

    [Range(0f, 0.25f)]
    public float RotationSpring = 3.0f;
    public float RotateDamping = 1.0f;
    
    public float YDistanceToFeet = 1.0f;
    public float FeetSpring = 5.0f;
    public float FeetDamping = 1.0f;

	// Update is called once per frame
	void Update ()
    {
        FootLoseTimer = Mathf.Min(FootLoseTimer + Time.deltaTime, FootLoseTime);
        FootOnGroundTimer = Mathf.Min(FootOnGroundTimer + Time.deltaTime, FootOnGroundTime);


        float heightestYFoot = 0f;
        for (int i = 0; i < legs.Length; i++)
        {
            Leg leg = legs[i];

            if(Application.isPlaying)
                leg.Update();

            float y = leg.Foot.position.y;

            if (i == 0 || y > heightestYFoot)
                heightestYFoot = y;

            if (Debug)
                leg.DebugRestrictionArea();
        }
	}

    public bool CanMove
    {
        get
        {
            return (feetOnGround >= 2);
        }
    }

    void FixedUpdate()
    {
        if (!Application.isPlaying)
            return;

        float delta = Time.fixedDeltaTime / 0.02f;

        Vector3 velocity = body.rigidbody.velocity;

        Vector3 moveDirection = Input.GetAxis("Vertical") * body.forward;
        if (CanMove)
            velocity += moveDirection * MoveSpeed * delta;
        Vector3 rotation = Input.GetAxis("Horizontal") * Vector3.up;
        if (CanMove)
            body.rigidbody.angularVelocity += rotation * RotateSpeed * delta;

        Vector3 currentVector = body.up;
        Vector3 targetVector = body.up;
        targetVector.x = 0;
        targetVector.z = 0;

        float cosAngle;
        Vector3 crossResult;
        float turnAngle;

        float averageYFoot = 0f;
        float heightestYFoot = 0f;
        for (int i = 0; i < legs.Length; i++)
        {
            Leg leg = legs[i];
            //leg.Update();

            float y = leg.Foot.position.y;

            averageYFoot += (y - body.position.y) + YDistanceToFeet;
            if (i == 0 || y > heightestYFoot)
                heightestYFoot = y;

            //float upForce = (body.position.y + YDistanceToFeet) - y;
            //upForce = Mathf.Min(FeetDamping, upForce);

            Vector3 targetV = leg.Foot.position - body.position;
            //targetV = Quaternion.LookRotation(-targetV).eulerAngles;
            //targetV.y = 0;

            cosAngle = Vector3.Dot(currentVector, targetV);

            crossResult = Vector3.Cross(currentVector, targetV);
            crossResult.Normalize();

            turnAngle = Mathf.Acos(cosAngle);
            turnAngle = Mathf.Min(turnAngle, FeetDamping);
            turnAngle = turnAngle * Mathf.Rad2Deg;

            //crossResult = Vector3.Reflect(currentVector, targetV);

            //targetVector += crossResult * turnAngle * FeetSpring * delta;

            //Vector3 cross = Vector3.Cross(body.up, targetV);
            //cross.Normalize();
            //body.rigidbody.AddForceAtPosition(Vector3.up * upForce * FeetSpring, leg.Root.position);
        }
        averageYFoot /= legs.Length;


        cosAngle = Vector3.Dot(currentVector, targetVector);

        crossResult = Vector3.Cross(currentVector, targetVector);
        crossResult.Normalize();

        turnAngle = Mathf.Acos(cosAngle);
        turnAngle = Mathf.Min(turnAngle, RotateDamping);
        turnAngle = turnAngle * Mathf.Rad2Deg;

        body.rigidbody.angularVelocity += crossResult * turnAngle * RotationSpring * delta;


        Vector3 groundPos = body.position;
        groundPos += Vector3.up * averageYFoot;
        velocity += (groundPos - body.position) * HeightSpring * delta;
        
        body.rigidbody.angularVelocity -= body.rigidbody.angularVelocity * RotateVelocityDamping * delta;
        velocity.y -= velocity.y * HeightDamping * delta;
        velocity -= velocity * VelocityDamping * delta;
        body.rigidbody.velocity = velocity;
    }

    private bool GetGround(Vector3 position, out Vector3 hitPoint)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 10f, FootWalkLayerMask))
        {
            hitPoint = hit.point;
            return true;
        }
        hitPoint = position;
        return false;
    }
}