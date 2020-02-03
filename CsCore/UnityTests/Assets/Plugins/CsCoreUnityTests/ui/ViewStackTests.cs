﻿using com.csutil.ui;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace com.csutil.tests.ui {

    class ViewStackTests {

        [UnityTest]
        public IEnumerator ExampleUsage1() {

            var viewStackGo = new GameObject();
            var viewStack = viewStackGo.AddComponent<ViewStack>();

            // Views can be added manually without using the ViewStack:
            var view1 = viewStackGo.AddChild(new GameObject("View 1"));
            // You can get the ViewStack using any child gameobject:
            Assert.AreEqual(view1.GetViewStack(), viewStack);
            // The latest active view can be accessed from the view stack:
            Assert.AreEqual(view1, viewStack.GetLatestView());

            // Views can also be added using the ViewStack.ShowView method:
            var view2 = viewStack.ShowView(new GameObject("View 2"));
            // Hide the old view 1 now that view 2 is on top:
            view1.SetActiveV2(false);
            Assert.IsFalse(view1.activeInHierarchy);
            Assert.AreEqual(view2, viewStack.GetLatestView());

            // The ViewStack can be used to return to the last view:
            Assert.IsTrue(viewStack.SwitchBackToLastView(view2));
            // View 2 will be removed from the view stack by destroying it:
            Assert.IsTrue(view2.IsDestroyed());
            // Now view 1 is active and visible again:
            Assert.IsTrue(view1.activeInHierarchy);

            yield return null;
        }

        [UnityTest]
        public IEnumerator TestViewStack1() {

            var viewStackGo = new GameObject();
            viewStackGo.AddComponent<ViewStack>();

            var view1 = viewStackGo.AddChild(new GameObject("View 1"));
            Assert.AreEqual(viewStackGo.GetComponent<ViewStack>(), view1.GetViewStack());

            var view2 = view1.GetViewStack().ShowView(new GameObject("View 2"));
            view1.SetActiveV2(false);
            Assert.IsFalse(view1.activeInHierarchy);

            var view3 = view1.GetViewStack().ShowView(new GameObject("View 3"));
            Assert.IsTrue(view2.activeInHierarchy);

            // Now test SwitchBackToLastView plus the events the view stack sends out:
            EventBus.instance.Subscribe(this, EventConsts.SWITCH_BACK_TO_LAST_VIEW, (string currentViewName, GameObject lastView) => {
                Assert.AreSame(view2, lastView);
                EventBus.instance.Unsubscribe(this, EventConsts.SWITCH_BACK_TO_LAST_VIEW);
            });
            Assert.IsTrue(view3.GetViewStack().SwitchBackToLastView(view3));
            Assert.IsTrue(view3.IsDestroyed());
            Assert.IsTrue(view2.activeInHierarchy);

            Assert.IsTrue(view2.GetViewStack().SwitchBackToLastView(view2));
            Assert.IsTrue(view2.IsDestroyed());
            Assert.IsTrue(view1.activeInHierarchy);

            Assert.IsFalse(view1.GetViewStack().SwitchBackToLastView(view1));
            Assert.IsTrue(view1.activeInHierarchy); // view 1 still active because its the last one

            yield return null;
        }
    }
}
