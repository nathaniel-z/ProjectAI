﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIInput : MonoBehaviour
{
    public float guidePointDistance;
    public float feelerRadius;
    public float wanderStrength;
    public Path path;
    public Transform car;
    public CarBehaviour behaviour;
    
    // state machine vars
    private InputStateMachine stateMachine;
    private IState normalState;

    void Awake()
    {
        stateMachine = new InputStateMachine();
        normalState = new NormalState(this, wanderStrength);
        stateMachine.ChangeState(normalState);
    }

    void Update()
    {
        behaviour.ApplyInput(stateMachine.Update());
    }

    public float PathFollowInput()
    {
        PathPointInfo goal = path.FindClosestLeadingPoint(car.position, guidePointDistance);

        // match car height to goal height
        Vector3 carPos = car.position;
        carPos.y = goal.point.y;

        // get perpendicular line to the guide point's direction
        Vector3 perpendicular = new Vector3(goal.direction.z, 0, -goal.direction.x);

        bool doTurn = false;

        // find the intersection between the car's heading and perpendicular to the track
        Vector3 intersect = VectorUtil.GetLineIntersectionPoint(carPos, carPos + car.forward, goal.point, goal.point + perpendicular, out bool found);
        // also turn if the intercept is behind the car, this means the car is backward
        if (found && Vector3.Dot(car.forward, intersect - carPos) >= 0)
        {
            // if there is an intercept, that is the feeler
            // check if the feeler falls outside the path
            intersect.y = goal.point.y;
            float feelerDistance = (intersect - goal.point).magnitude;
            if (feelerDistance + feelerRadius > path.radius)
            {
                // if it does, we're going to hit a wall, time to correct
                doTurn = true;
            }
        }
        else
        {
            // no intercept found, steer toward the goal
            doTurn = true;
        }

        // if we do need to turn, return the direction
        if (doTurn)
        {
            float dir = Vector3.Dot(car.right, goal.point - carPos);
            if (Mathf.Abs(dir) > Mathf.Epsilon)
            {
                return dir / Mathf.Abs(dir);
            }
        }

        // don't need to turn
        return 0;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        PathPointInfo goal = path.FindClosestLeadingPoint(car.position, guidePointDistance);
        Vector3 perpendicular = new Vector3(goal.direction.z, 0, -goal.direction.x);
        Vector3 carPos = car.position;
        carPos.y = goal.point.y;

        // draw goal point feeler
        Vector3 intersect = VectorUtil.GetLineIntersectionPoint(car.position, car.position + car.forward, goal.point, goal.point + perpendicular, out bool found);
        if (found && Vector3.Dot(car.forward, intersect - carPos) >= 0)
        {
            Vector3 vehiclePoint = car.position;
            vehiclePoint.y = intersect.y = goal.point.y;
            Gizmos.color = Color.red;
            Gizmos.DrawLine(goal.point, intersect);
            Gizmos.DrawLine(vehiclePoint, intersect);
            Gizmos.DrawWireSphere(intersect, feelerRadius);
        }

        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(goal.point, 0.25f);
        Gizmos.DrawLine(goal.point, goal.point + goal.direction);
    }
#endif
}