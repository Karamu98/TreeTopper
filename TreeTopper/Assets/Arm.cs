using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arm : MonoBehaviour
{
    [SerializeField] private Player m_base = default;

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<GrabPoint>(out GrabPoint grabPoint))
        {
            m_base.GrabCollide(grabPoint);
        }
    }
}
