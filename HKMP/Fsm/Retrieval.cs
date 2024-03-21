using System;
using System.Collections.Generic;
using UnityEngine;
using Hkmp.Util;
using Hkmp.Fsm;
using Hkmp.Networking.Client;

namespace Hkmp
{
    public class Retrieval
    {
        public Retrieval()
        {
        }

        public static string[] ChildrenList(GameObject gameObject)
        {
            List<string> childNames = new List<string>();

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                // Access the i-th child GameObject's Transform component
                Transform childTransform = gameObject.transform.GetChild(i);
                // Add name of it to list
                childNames.Add(childTransform.gameObject.name);
            }

            return childNames.ToArray();
        }

        public static string[] ComponentList(GameObject gameObject)
        {
            Component[] components = gameObject.GetComponents<Component>();

            List<string> componentNames = new List<string>();
            // Iterate through all components
            foreach (Component component in components)
            {
                // Check if the component is a MonoBehaviour (script)
                if (component is MonoBehaviour)
                {
                    // Found a script, you can now access its methods and properties
                    MonoBehaviour script = (MonoBehaviour)component;
                    componentNames.Add(script.GetType().Name);
                }
            };

            return componentNames.ToArray();
        }
    }
}

