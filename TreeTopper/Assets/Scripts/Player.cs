using Cinemachine;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] Transform m_armSocket = default;
    [SerializeField] AudioClip m_jumpClip = default;
    [SerializeField] AudioClip m_deathClip = default;
    [SerializeField] AudioClip m_landClip = default;

    private void OnTriggerEnter(Collider other)
    {
        if(m_curState == State.Flying)
        {
            if (other.TryGetComponent<GrabPoint>(out GrabPoint grabPoint))
            {
                GrabCollide(grabPoint);
            }
        }
    }

    public void GrabCollide(GrabPoint grabPoint)
    {
        if (m_curState == State.Flying)
        {
            m_rb.isKinematic = true;
            StartSwingAboutPoint(grabPoint.Grab());
            m_audioSource.PlayOneShot(m_landClip);
            TransitionState(State.Landing);

            if (grabPoint.Index != GrabPointManager.GrabPoints.Count - 1)
            {
                int nextIDX = grabPoint.Index + 1;
                m_nextGrabPoint = GrabPointManager.GrabPoints[nextIDX].transform;
                m_targetGroup.AddMember(m_nextGrabPoint, 1, 8);
            }
            else
            {
                Camera.main.backgroundColor = Color.blue;
                Reset();
            }
        }
    }

    private void Start()
    {
        StartSwingAboutPoint(GrabPointManager.GrabPoints[0].Grab());
        m_rb = GetComponent<Rigidbody>();
        m_audioSource = GetComponent<AudioSource>();
        m_lastPos = transform.position;
        m_targetGroup = FindObjectOfType<CinemachineTargetGroup>();
        m_cam = Camera.main;
    }

    private void Update()
    {
        switch(m_curState)
        {
            case State.Start:
                {
                    if(Input.anyKeyDown)
                    {
                        TransitionState(State.Swing);
                    }
                }
                break;

            case State.Swing: Swinging(); break;

            case State.Flying: Flying(); break;
        }

        // Any state
        if(Input.GetKeyDown(KeyCode.R))
        {
            Reset();
        }
    }

    private void FixedUpdate()
    {
        if(m_curState == State.Swing)
        {
            CalculateAvgSpeed();   
        }
    }

    private void CalculateAvgSpeed()
    {
        float avg = 0.0f;
        for(int i = 0; i < m_speedBufferLen; ++i)
        {
            avg += m_speedBuffer[i];
        }

        m_avgSpeed = avg / m_speedBufferLen;
    }

    private void StartSwingAboutPoint(Transform newPoint)
    {
        m_curSwingPoint = newPoint;

        transform.up = (m_curSwingPoint.position - transform.position).normalized;
        m_armSocket.transform.right = transform.up;
    }

    public void Reset()
    {
        foreach (GrabPoint point in GrabPointManager.GrabPoints)
        {
            point.Reset();
        }

        m_audioSource.PlayOneShot(m_deathClip);

        StartSwingAboutPoint(GrabPointManager.GrabPoints[0].Grab());
        m_curState = State.Swing;
        m_rb.isKinematic = true;

        m_targetGroup.m_Targets = new CinemachineTargetGroup.Target[0];
        m_targetGroup.AddMember(GrabPointManager.GrabPoints[0].transform, 2, 8);

        m_curSwingSpeed = m_startSwingSpeed;
    }

    private void Swinging()
    {
        if(Input.GetKey(KeyCode.D))
        {
            m_curSwingSpeed = Mathf.Clamp(m_curSwingSpeed + (m_swingIncrementSpeed * Time.deltaTime), m_startSwingSpeed, m_maxSwingSpeed);
        }

        if(Input.GetKeyDown(KeyCode.Space))
        {
            m_audioSource.PlayOneShot(m_jumpClip);
            TransitionState(State.Jumping);
            return;
        }

        float maxY = m_curSwingPoint.position.y - m_swingRadius;
        float startY = m_curSwingPoint.position.y + (m_swingRadius * m_apexNormal);
        float speedMod = (transform.position.y - startY / maxY - startY);

        float curSpeed = Mathf.Lerp(m_curSwingSpeed, m_curSwingSpeed * m_apexSwingSpeed, speedMod);

        transform.RotateAround(m_curSwingPoint.position, Vector3.forward, curSpeed * Time.deltaTime);
        Vector3 delta = transform.position - m_curSwingPoint.position;
        delta.z = 0;
        transform.position = m_curSwingPoint.position + delta.normalized * m_swingRadius;

        m_curDir = -((m_lastPos - transform.position) / Time.deltaTime);
        m_speedBuffer[m_curSpeedIDX % m_speedBufferLen] = m_curDir.magnitude;
        ++m_curSpeedIDX;

        m_lastPos = transform.position;
    }

    private void Flying()
    {
        Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        worldMousePos.z = m_armSocket.position.z;

        m_armSocket.right = -(m_armSocket.position - worldMousePos).normalized;
    }

    private void OnGUI()
    {
        GUILayout.Label($"Current State: {m_curState}");
        GUILayout.Label($"Speed: {m_avgSpeed.ToString("0.0")}");
    }

    private void TransitionState(State newState)
    {
        switch(newState)
        {
            case State.Landing:
                {
                    if(m_curState != State.Flying)
                    {
                        Debug.LogError($"State error, expected State.Flying not ${m_curState}");
                    }

                    m_targetGroup.RemoveMember(transform);
                    m_targetGroup.RemoveMember(m_nextGrabPoint);

                    m_curState = newState;
                    TransitionState(State.Swing);
                }
                break;

            case State.Swing:
                {
                    if(!(m_curState == State.Landing || m_curState == State.Start))
                    {
                        Debug.LogError($"State error, expected State.Landing or State.Start not ${m_curState}");
                    }

                    m_targetGroup.AddMember(m_curSwingPoint, 1, 8);

                    m_curState = newState;
                }
                break;

            case State.Jumping:
                {
                    if (m_curState != State.Swing)
                    {
                        Debug.LogError($"State error, expected State.Swing not ${m_curState}");
                    }

                    m_targetGroup.RemoveMember(m_curSwingPoint);
                    m_targetGroup.AddMember(transform, 2, 8);

                    m_curSwingPoint = null;

                    m_rb.isKinematic = false;
                    m_rb.velocity = Vector3.zero;
                    m_rb.AddForce(m_curDir.normalized * m_avgSpeed, ForceMode.VelocityChange);

                    m_curState = newState;
                    TransitionState(State.Flying);
                }
                break;

            case State.Flying:
                {
                    if (m_curState != State.Jumping)
                    {
                        Debug.LogError($"State error, expected State.Jumping not ${m_curState}");
                    }

                    m_curState = newState;
                }
                break;
        }
    }

    private State m_curState = State.Start;
    private const float m_startSwingSpeed = 600.0f;
    private const float m_swingRadius = 2.0f;
    private float m_curSwingSpeed = m_startSwingSpeed;
    private float m_swingIncrementSpeed = 400f;
    private float m_maxSwingSpeed = 500000.0f;
    private Transform m_curSwingPoint = null;
    private float m_apexSwingSpeed = 0.5f;
    private float m_apexNormal = 0.6f;

    private int m_curSpeedIDX = 0;
    private float[] m_speedBuffer = new float[m_speedBufferLen];
    private float m_avgSpeed;
    private const int m_speedBufferLen = 30;
    private Vector3 m_curDir;

    private Transform m_nextGrabPoint = null;
    private Vector3 m_lastPos;
    private CinemachineTargetGroup m_targetGroup;
    private AudioSource m_audioSource;
    private Camera m_cam;

    private Rigidbody m_rb;

    private enum State
    {
        Start,
        Swing,
        Flying,
        Jumping,
        Landing,
        Dead
    }
}
