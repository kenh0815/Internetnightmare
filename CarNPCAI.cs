using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;

[RequireComponent(typeof(NavMeshAgent))]
public class CarNPCAI : NetworkBehaviour
{
    public Transform[] wayPoints;
    public int currentWaypointIndex = 0;

    private NavMeshAgent agent;
    private bool isStopped = false;

    // Waypointに近づいたと判断する距離を調整可能にする
    public float waypointThreshold = 1.5f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!IsServer)
        {
            agent.enabled = false;
            enabled = false;
            return;
        }

        if (wayPoints == null || wayPoints.Length == 0)
        {
            Debug.LogError("Waypointsが未設定または空です！");
            enabled = false;
            return;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.autoBraking = false;  // ブレーキを無効にしてスムーズに通過
            agent.SetDestination(wayPoints[currentWaypointIndex].position);
        }
        else
        {
            Debug.LogError("NavMeshAgentが正しく設定されていないかNavMesh上にありません！");
        }
    }

    void Update()
    {
        if (isStopped || agent == null || !agent.enabled) return;

        if (!agent.pathPending && agent.remainingDistance <= waypointThreshold)
        {
            GoToNextWaypoint();
        }
    }

    void GoToNextWaypoint()
    {
        if (wayPoints == null || wayPoints.Length == 0) return;

        currentWaypointIndex = (currentWaypointIndex + 1) % wayPoints.Length;

        if (agent.isOnNavMesh)
        {
            agent.SetDestination(wayPoints[currentWaypointIndex].position);
        }
    }

    public void SetStopped(bool stopped)
    {
        isStopped = stopped;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            Debug.LogWarning("NavMeshAgentが利用できないため、停止/再開を行えません。");
            return;
        }

        agent.isStopped = stopped;

        if (!stopped)
        {
            agent.SetDestination(wayPoints[currentWaypointIndex].position);
        }
    }
}
