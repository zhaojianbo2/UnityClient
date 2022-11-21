using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static NetAdapter;

//Move by Screen Reference System
public class PlayerControl : MonoBehaviour
{
    //public Transform camera;
    //private Rigidbody rigidbody;
    public float moveSpeed = 4;
    public float jumpForce = 200f;
    Animator anim;
    private long playerId;
    NavMeshAgent m_MeshAgent;
    bool running = false;

    public void SetPlayer(GameObject player,long pid)
    {
        this.playerId = pid;
        anim = player.GetComponent<Animator>();
        m_MeshAgent = player.GetComponent<NavMeshAgent>();
        m_MeshAgent.speed = moveSpeed;
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        if (anim == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hitInfo, 100000))
            {
                if (anim.GetInteger("run") == 0)
                {
                    anim.SetInteger("run", 1);
                }
                StartCoroutine(InertialRun(hitInfo.point));

            }
        }
        if (running&&!m_MeshAgent.pathPending)
        {
            if (m_MeshAgent.remainingDistance <= m_MeshAgent.stoppingDistance)
            {
                if (!m_MeshAgent.hasPath || m_MeshAgent.velocity.sqrMagnitude == 0f)
                {
                    if (anim.GetInteger("run") == 1)
                    {
                        anim.SetInteger("run", 0);
                        running = false;
                    }
                }
            }

        }
    }
    public IEnumerator InertialRun(Vector3 point)
    {

        yield return new WaitForSeconds(0.3f);
        m_MeshAgent.SetDestination(point);
        ReqSceneObjRunMsg runMsg = new();
        runMsg.playerId = playerId;
        Vector3 current = m_MeshAgent.transform.position;
        PointInfo p1 = new PointInfo();
        p1.x = (int)current.x;
        p1.y = (int)current.z;
        runMsg.roads.Add(p1);

        PointInfo p2 = new PointInfo();
        p2.x = (int)point.x;
        p2.y = (int)point.z;
        runMsg.roads.Add(p2);
        NetWorkBehavior.Instance.Send<ReqSceneObjRunMsg>(runMsg, 1003);

        running = true;
    }
}