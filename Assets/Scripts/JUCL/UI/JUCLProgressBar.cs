using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JUCL.UI
{
    [ExecuteInEditMode()]
    public class JUCLProgressBar : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("JUCL/Create/UI/Linear Progress Bar")]
        public static void AddLinearProgressBar()
        {
            GameObject go = Instantiate(Resources.Load<GameObject>("UI/Linear Progress Bar"));
            go.transform.SetParent(Selection.activeGameObject.transform, false);
        }
#endif

        [SerializeField]
        int minimum = 0;
        [SerializeField]
        int maximum = 1;
        [SerializeField]
        int current = 0;
        [SerializeField]
        Image mask = null;
        [SerializeField]
        Image fill = null;
        [SerializeField]
        Color color = new Color(1.0f, 1.0f, 1.0f);
        [SerializeField]
        bool selfControlled = false;

        // Start is called before the first frame update
        void Start()
        {
            mask = transform.GetChild(0).GetComponent<Image>();
            fill = transform.GetChild(0).GetChild(0).GetComponent<Image>();
            fill.color = color;
        }

        // Update is called once per frame
        void Update()
        {
            if (selfControlled)
            {
                SetCurrentFill(current, minimum, maximum);
            }
        }

        public void SetCurrentFill(float current, float minimum, float maximum)
        {
            float currentOffset = current - minimum;
            float maximumOffset = maximum - minimum;
            float fillAmount = currentOffset / maximumOffset;
            mask.fillAmount = fillAmount;
        }

        public void SetBarColor(Color newColor)
        {
            color = newColor;
            fill.color = color;
        }

        //Turn progress bar on
        public void EnableBar()
        {
            gameObject.SetActive(true);
        }
        //Turn progress bar off
        public void DisableBar()
        {
            gameObject.SetActive(false);
        }

        public bool IsBarEnabled()
        {
            return gameObject.activeSelf;
        }
    }
}