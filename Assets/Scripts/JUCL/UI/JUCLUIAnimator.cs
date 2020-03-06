using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace JUCL.UI
{
    public class JUCLUIAnimator : MonoBehaviour
    {

        //Object to animate
        [SerializeField] GameObject objectToAnimate = null;
        [SerializeField] JUCLUIAnimationAsset animationSettings = null;

        private LTDescr tweenObject;

        //Function to check whether the parent is suitable for this animation to work correctly
        public void CheckForIdealTweenConditions()
        {
            //If this object is not set yet
            if (objectToAnimate != null)
            {
                if (objectToAnimate.transform.parent != null)
                {
                    //Get parent object
                    GameObject parentObject = objectToAnimate.transform.parent.gameObject;
                    if (parentObject != null)
                    {
                        //Try to find a layout group on the parent
                        LayoutGroup parentLayoutGroup = parentObject.GetComponent<LayoutGroup>();
                        if (parentLayoutGroup != null)
                        {
                            Debug.LogWarning("Parent of UI Animator should not have any layout groups as it can cause tweening to function incorrectly");
                        }
                    }
                }
            }
        }

        //When game object enabled
        public void OnEnable()
        {
            if (animationSettings != null)
            {
                //If animation should be shown
                if (animationSettings.showOnEnable)
                {
                    //Show it
                    Show();
                }
            }
        }

        //Show animation
        public void Show()
        {
            if (animationSettings != null)
            {
                //Null pointer check and cancel tween if one already in progress
                if (objectToAnimate == null)
                {
                    objectToAnimate = gameObject;
                }
                else
                {
                    LeanTween.cancel(objectToAnimate);
                }

                CheckForIdealTweenConditions();

                HandleTween(false, animationSettings.inAnimationType, animationSettings.inEaseType, animationSettings.inDelay,
                    animationSettings.inDuration, animationSettings.inLoop, animationSettings.inPingpong, animationSettings.inStartPositionOffset);

                //Turns object to active if object is actually tweening
                if (LeanTween.isTweening(objectToAnimate))
                {
                    objectToAnimate.SetActive(true);
                }
            }
        }

        //Function to disable the animations
        public void Disable()
        {
            if (animationSettings != null)
            {
                //Null pointer check cancel tween if one already in progress
                if (objectToAnimate == null)
                {
                    objectToAnimate = gameObject;
                }
                else
                {
                    LeanTween.cancel(objectToAnimate);
                }

                CheckForIdealTweenConditions();

                //Tween handle
                HandleTween(true, animationSettings.outAnimationType, animationSettings.outEaseType, animationSettings.outDelay,
                    animationSettings.outDuration, animationSettings.outLoop, animationSettings.outPingpong, animationSettings.outStartPositionOffset);
                //Run upon completion
                tweenObject.setOnComplete(() =>
                {
                    if (animationSettings.destroyOnDisable)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        gameObject.SetActive(false);
                    }
                });
            }
        }

        //Handles actual tween logic
        public void HandleTween(bool isDisabling, UIAnimationTypes animationType, LeanTweenType easeType, float delay, float duration, bool loop, bool pingpong, bool startPositionOffset)
        {
            //Checks whether this script should be animating this object
            if (objectToAnimate == null)
            {
                objectToAnimate = gameObject;
            }

            //Switch for different animation types
            switch (animationType)
            {
                //Fade 
                case UIAnimationTypes.Fade:
                    if (isDisabling)
                    {
                        //Fade out
                        Fade(animationSettings.outFrom, animationSettings.outTo, startPositionOffset, duration);
                    }
                    else
                    {
                        //Fade in
                        Fade(animationSettings.inFrom, animationSettings.inTo, startPositionOffset, duration);
                    }
                    break;
                //Move
                case UIAnimationTypes.Move:
                    if (isDisabling)
                    {
                        //Move out
                        MoveAbsolute(animationSettings.outFrom, animationSettings.outTo, duration);
                    }
                    else
                    {
                        //Move in
                        MoveAbsolute(animationSettings.inFrom, animationSettings.inTo, duration);
                    }
                    break;
                //Scale
                case UIAnimationTypes.Scale:
                    if (isDisabling)
                    {
                        //Scale out
                        Scale(animationSettings.outFrom, animationSettings.outTo, startPositionOffset, duration);
                    }
                    else
                    {
                        //Scale in
                        Scale(animationSettings.inFrom, animationSettings.inTo, startPositionOffset, duration);
                    }
                    break;
            }

            //Tween settings
            tweenObject.setDelay(delay);
            tweenObject.setEase(easeType);

            //Loop settings
            if (loop)
            {
                tweenObject.loopCount = int.MaxValue;
            }

            //Ping pong effect
            if (pingpong)
            {
                tweenObject.setLoopPingPong();
            }
        }

        //Fade function
        public void Fade(Vector3 from, Vector3 to, bool startPositionOffset, float duration)
        {
            //Canvas group check for alpha channel
            if (gameObject.GetComponent<CanvasGroup>() == null)
            {
                gameObject.AddComponent<CanvasGroup>();
            }

            //Checks whether it starts at a different alpha
            if (startPositionOffset)
            {
                objectToAnimate.GetComponent<CanvasGroup>().alpha = from.x;
            }
            //Apply alpha change
            tweenObject = LeanTween.alphaCanvas(objectToAnimate.GetComponent<CanvasGroup>(), to.x, duration);
        }

        //Move object
        public void MoveAbsolute(Vector3 from, Vector3 to, float duration)
        {
            objectToAnimate.GetComponent<RectTransform>().anchoredPosition = from;
            tweenObject = LeanTween.move(objectToAnimate.GetComponent<RectTransform>(), to, duration);
        }

        //Scale object
        public void Scale(Vector3 from, Vector3 to, bool startPositionOffset, float duration)
        {
            if (startPositionOffset)
            {
                objectToAnimate.GetComponent<RectTransform>().localScale = from;
                tweenObject = LeanTween.scale(objectToAnimate, to, duration);
            }
        }
    }

    public enum UIAnimationTypes
    {
        Move,
        Fade,
        Scale
    }
}