using UnityEngine;

namespace AIinGames
{
    public class Painter : MonoBehaviour
    {
        [SerializeField]
        private PaintReceiver m_InitialPaintReceiver;

        [SerializeField]
        private float m_RaycastLength = 500f;

        [SerializeField]
        private float m_PaintingDistance = 2f;

        [SerializeField]
        private float m_Strength = 0.8f;

        [SerializeField]
        private float m_Spacing = 1f;

        [SerializeField]
        private Color m_InitialColor = Color.white;

        private float m_CurrentAngle = 0f;
        private float m_LastAngle = 0f;

        private PaintReceiver m_PaintReceiver;
        private Collider m_PaintReceiverCollider;

        private Color m_CurrentColor;

        private Vector2? m_LastDrawPosition = null;

        private void Start()
        {
            ChangeColour(m_InitialColor);

            Initialize(m_InitialPaintReceiver);
        }

        public void Initialize(PaintReceiver newPaintReceiver)
        {
            m_PaintReceiver = newPaintReceiver;
            m_PaintReceiverCollider = newPaintReceiver.GetComponent<Collider>();
        }

        private void Update()
        {
            if (m_PaintReceiver == null)
                return;

            if (Input.GetMouseButtonDown(1))
            {
                m_PaintReceiver.ResetColor();

                return;
            }

            if (!Input.GetMouseButton(0))
            {
                m_LastDrawPosition = null;
                return;
            }

            m_CurrentAngle = -transform.rotation.eulerAngles.z;


            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            Debug.DrawRay(ray.origin, ray.direction * m_RaycastLength);

            if (m_PaintReceiverCollider.Raycast(ray, out hit, m_RaycastLength))
            {
                if (m_LastDrawPosition.HasValue && m_LastDrawPosition.Value != hit.textureCoord)
                {
                    m_PaintReceiver.DrawLine(m_LastDrawPosition.Value, hit.textureCoord, m_LastAngle, m_CurrentAngle, m_CurrentColor, m_PaintingDistance, m_Strength, m_Spacing);
                }
                else
                {
                    m_PaintReceiver.CreateSplash(hit.textureCoord, m_CurrentColor, m_PaintingDistance, m_Strength);
                }

                m_LastAngle = m_CurrentAngle;

                m_LastDrawPosition = hit.textureCoord;
            }
            else
            {
                m_LastDrawPosition = null;
            }
        }

        public void ChangeColour(Color newColor)
        {
            m_CurrentColor = newColor;
        }

        public void SetRotation(float newAngle)
        {
            m_CurrentAngle = newAngle;
        }
    }
}
