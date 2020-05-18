using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Barracuda;

namespace AIinGames
{
    public class BarracudaTest : MonoBehaviour
    {
        [SerializeField]
        private PaintReceiver m_PaintReceiver;

        [SerializeField]
        private NNModel m_Model;

        [SerializeField]
        private WorkerFactory.Type m_WorkerType = WorkerFactory.Type.ComputePrecompiled;

        [SerializeField]
        private Text[] m_ResultsArray;

        [SerializeField]
        private GameObject m_TestButtonPrefab;

        [SerializeField]
        private Transform m_TestButtonParent;

        private string[] m_Labels = { "alarm clock", "ant", "asparagus", "baseball", "bee", "broom", "carrier", "crocodile", "dolphin", "phone" };

        private BarracudaWorker m_BarracudaWorker;
        
        private void Start()
        {
            m_PaintReceiver.OnUpdatedTexture += OnUpdatedTexture;

            InitializeTestButtons();

            m_BarracudaWorker = new BarracudaWorker(m_Model, m_WorkerType);
        }

        private void InitializeTestButtons()
        {
            m_PaintReceiver.InitializeTestImages(m_Labels);

            foreach (string label in m_Labels)
            {
                GameObject newTestButton = Instantiate<GameObject>(m_TestButtonPrefab, m_TestButtonParent);
                newTestButton.GetComponentInChildren<Text>().text = label;
                newTestButton.GetComponent<Button>().onClick.AddListener(() =>
                {
                    m_PaintReceiver.SetToTestTexture(label);
                });
            }
        }

        private void OnDestroy()
        {
            m_BarracudaWorker.Dispose();
        }

        private void OnUpdatedTexture(Texture2D texture)
        {
            if (!m_IsBusy)
            {
                StartCoroutine(ExecuteAsync(texture));
            }
        }

        private bool m_IsBusy = false;

        IEnumerator ExecuteAsync(Texture2D inputTex)
        {
            if (m_IsBusy)
                yield break;

            m_IsBusy = true;

            yield return m_BarracudaWorker.ExecuteAsyncTexture(inputTex);

            var result = m_BarracudaWorker.GetResult();

            Dictionary<int, float> results = new Dictionary<int, float>();
            for (int i = 0; i < result.Length; i++)
            {
                results[i] = result[i];
            }
            for (int i = 0; i < result.Length; i++)
            {
                int largestElement = -1;
                float largestValue = -1f;
                foreach (KeyValuePair<int, float> keyValuePair in results)
                {
                    if (keyValuePair.Value >= largestValue)
                    {
                        largestValue = keyValuePair.Value;
                        largestElement = keyValuePair.Key;
                    }
                }

                m_ResultsArray[i].text = $"{m_Labels[largestElement]}: {largestValue}";
                results.Remove(largestElement);
            }

            m_IsBusy = false;
        }
    }
}
