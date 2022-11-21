using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapParams : MonoBehaviour
{
    public static MapParams Current;
    [HideInInspector]
    public List<Vector3> HeroTankPatrolPositions;
    public Transform BossReleaseMonster;
    public Light[] Lights = new Light[0];
    LightShadows[] m_OriginLightShadows;
    private void Awake()
    {
        HeroTankPatrolPositions = new List<Vector3>();
        Current = this;
        for (int i = 0; i < transform.childCount; i++)
        {
            HeroTankPatrolPositions.Add(transform.GetChild(i).position);
        }
        if (BossReleaseMonster == null)
            BossReleaseMonster = transform;
        m_OriginLightShadows = new LightShadows[Lights.Length];
        for (int i = 0; i < m_OriginLightShadows.Length; i++)
        {
            if (Lights[i] == null)
                continue;
            m_OriginLightShadows[i] = Lights[i].shadows;
        }
    }

    public void RefreshLights()
    {
        for (int i = 0; i < Lights.Length; i++)
        {
            if (Lights[i] == null)
                continue;
            LightShadows shadows = LightShadows.None;
            Lights[i].shadows = shadows;
        }
    }
}
