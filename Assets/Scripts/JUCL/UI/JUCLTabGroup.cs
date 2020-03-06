using System.Collections.Generic;
using UnityEngine;

namespace JUCL.UI
{
    public class JUCLTabGroup : MonoBehaviour
    {
        //Settings
        [SerializeField] List<JUCLTabButton> tabButtons = null;
        [SerializeField] Color tabIdleColour = new Color();
        [SerializeField] Color tabHoverColour = new Color();
        [SerializeField] Color tabActiveColour = new Color();
        [SerializeField] JUCLTabButton selectedTab = null;
        [SerializeField] bool handleTabEnabled = true;
        [SerializeField] bool handleTabDisabled = true;
        [SerializeField] bool canReselectTabs = true;
        [SerializeField] List<GameObject> objectsToSwap = null;

        //Function called on all tabbuttons to add them to this grouping
        public void Subscribe(JUCLTabButton button)
        {
            if (tabButtons == null)
            {
                tabButtons = new List<JUCLTabButton>();
            }
            tabButtons.Add(button);
        }

        //When the tab is hovered over
        public void OnTabEnter(JUCLTabButton button)
        {
            //Reset tabs
            ResetTabColours();
            //Change to hover colour
            if (selectedTab == null || button != selectedTab)
            {
                button.background.color = tabHoverColour;
            }
        }

        //When the tab is no longer hovered over
        public void OnTabExit(JUCLTabButton button)
        {
            //Reset tabs
            ResetTabColours();
        }

        //When the tab is selected
        public void OnTabSelected(JUCLTabButton button)
        {
            //If tabs cannot be reselected
            if (!canReselectTabs)
            {
                //If new tab is equal to current tab
                if (button == selectedTab)
                {
                    //return out of function
                    return;
                }
            }

            canReselectTabs = false;

            //Callback on deselect
            if (selectedTab != null)
            {
                selectedTab.Deselect();
            }

            //Assign selected tab
            selectedTab = button;

            //Select callback
            selectedTab.Select();

            //Reset tabs
            ResetTabColours();
            //Set to active colour
            button.background.color = tabActiveColour;
            //Get page index for that tab
            int index = button.transform.GetSiblingIndex();
            //Set page index active
            for (int i = 0; i < objectsToSwap.Count; i++)
            {
                if (i == index)
                {
                    if (handleTabEnabled)
                    {
                        //Try to get animator
                        JUCLUIAnimator animator = objectsToSwap[i].GetComponent<JUCLUIAnimator>();
                        //If animator is present
                        if (animator != null)
                        {
                            //Run show animation
                            animator.Show();
                        }
                        else
                        {
                            //Disable body window
                            objectsToSwap[i].SetActive(true);
                        }
                    }
                }
                else
                {
                    //If tab should be disabled automatically
                    if (handleTabDisabled)
                    {
                        //Try to get animator
                        JUCLUIAnimator animator = objectsToSwap[i].GetComponent<JUCLUIAnimator>();
                        //If animator is present
                        if (animator != null)
                        {
                            //Run disable animation
                            animator.Disable();
                        }
                        else
                        {
                            //Disable body window
                            objectsToSwap[i].SetActive(false);
                        }
                    }
                }
            }
        }

        //Function to reset tabs
        public void ResetTabColours()
        {
            //For every tab button
            foreach (JUCLTabButton button in tabButtons)
            {
                //If this button is selected, skip it
                if (selectedTab != null && button == selectedTab) { continue; }
                //Set to idle colour
                button.background.color = tabIdleColour;
            }
        }

        //Function to disable the tab system
        public void DisableTabSystem()
        {
            //Set selected tab to null
            selectedTab = null;
            //For every main body window
            for (int i = 0; i < objectsToSwap.Count; i++)
            {
                //Call deselect callback on tab
                if (i < tabButtons.Count)
                {
                    if (tabButtons[i] != null)
                    {
                        tabButtons[i].Deselect();
                    }
                }

                //If disable tabs on switch mode
                if (handleTabDisabled)
                {
                    //Try to get animator
                    JUCLUIAnimator animator = objectsToSwap[i].GetComponent<JUCLUIAnimator>();
                    //If animator is present
                    if(animator != null)
                    {
                        //Run disable animation
                        animator.Disable();
                    }
                    else
                    {
                        //Disable body window
                        objectsToSwap[i].SetActive(false);
                    }
                }
            }
            //Reset the tab colours
            ResetTabColours();
        }

        //Function to set the tab reselection variable, useful for when menu's break out of the tab system
        public void SetTabReselection(bool status)
        {
            canReselectTabs = status;
        }
    }
}