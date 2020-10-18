using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrabPointManager : MonoBehaviour
{
    [SerializeField] List<GrabPoint> m_grabPoints;
    public static IReadOnlyList<GrabPoint> GrabPoints;


    private void Awake()
    {
        GrabPoints = m_grabPoints;

        for(int i = 0; i< m_grabPoints.Count; ++i)
        {
            m_grabPoints[i].Init(i);
        }
    }

    public void Reset()
    {
        foreach(GrabPoint point in m_grabPoints)
        {
            point.Reset();
        }
    }
}
