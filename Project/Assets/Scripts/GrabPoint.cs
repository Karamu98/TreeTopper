using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GrabPoint : MonoBehaviour
{
    [SerializeField] private Transform m_swingPoint;
    [SerializeField] private Collider m_collider;

    public int Index { get; private set; }

    public Transform Grab()
    {
        m_collider.enabled = false;
        return m_swingPoint;
    }

    public void Init(int index) => Index = index;

    public void Reset()
    {
        m_collider.enabled = true;
    }
}
