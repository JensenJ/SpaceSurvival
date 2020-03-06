using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace JUCL.UI
{
    //Requires an image
    [RequireComponent(typeof(Image))]
    //Implements mouse cursor events
    public class JUCLTabButton : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler, IPointerExitHandler
    {
        //Tabgroup and image
        public JUCLTabGroup tabGroup;
        public Image background;

        //Callback events
        public UnityEvent onTabSelected;
        public UnityEvent onTabDeselected;

        //When tab selected
        public void OnPointerClick(PointerEventData eventData)
        {
            //Manage on tab group
            tabGroup.OnTabSelected(this);
        }

        //When tab hovered over
        public void OnPointerEnter(PointerEventData eventData)
        {
            //Manage on tab group
            tabGroup.OnTabEnter(this);
        }

        //When tab not hovered over
        public void OnPointerExit(PointerEventData eventData)
        {
            //Manage on tab group
            tabGroup.OnTabExit(this);
        }

        // Start is called before the first frame update
        void Start()
        {
            //Button setup
            background = GetComponent<Image>();
            //If tab group is not assigned
            if (tabGroup == null)
            {
                //Try to find tabgroup on parent
                JUCLTabGroup tempGroup = transform.parent.GetComponent<JUCLTabGroup>();
                if (tempGroup != null)
                {
                    //Assign tab group
                    tabGroup = tempGroup;
                    //Subscribe to tab group events
                    tabGroup.Subscribe(this);
                }
                else
                {
                    //Warn user
                    Debug.LogError("TabButton does not have tab group set");
                }
            }
            else
            {
                //Subscribe to tab group events
                tabGroup.Subscribe(this);
            }
        }
        //Selection callback
        public void Select()
        {
            if (onTabSelected != null)
            {
                //Invoke callback
                onTabSelected.Invoke();
            }
        }

        //Deselection callback
        public void Deselect()
        {
            if (onTabDeselected != null)
            {
                //Invoke callback
                onTabDeselected.Invoke();
            }
        }
    }
}