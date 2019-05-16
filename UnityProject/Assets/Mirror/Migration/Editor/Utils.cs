using System;
using System.Reflection;
using UnityEngine;

namespace Mirror.MigrationUtilities {
    public class Utils : MonoBehaviour {

        public static int ReplaceNetworkComponent<TSource, TDestination>(GameObject prefab)
            where TSource : Component
            where TDestination : Component {

            int netComponentCount = 0;
            TSource unetNetworkComponent = prefab.GetComponent<TSource>();
            if (unetNetworkComponent != null) {
                netComponentCount++;

                // check for mirror component
                TDestination mirrorNetworkComponent = prefab.AddComponent<TDestination>();
                if (mirrorNetworkComponent == null) {
                    mirrorNetworkComponent = prefab.GetComponent<TDestination>();
                }

                // copy values
                CopyProperties(unetNetworkComponent, mirrorNetworkComponent);

                // destroy UNET component
                DestroyImmediate(unetNetworkComponent, true);
            }

            return netComponentCount;
        }

        // source: https://stackoverflow.com/questions/930433/apply-properties-values-from-one-object-to-another-of-the-same-type-automaticall
        static void CopyProperties(object source, object destination) {

            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");

            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            PropertyInfo[] srcProps = typeSrc.GetProperties();
            foreach (PropertyInfo srcProp in srcProps) {
                if (!srcProp.CanRead)
                    continue;

                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name,
                    BindingFlags.DeclaredOnly |
                    BindingFlags.Public |
                    BindingFlags.Instance);

                if (targetProperty == null)
                    continue;
                
                if (!targetProperty.CanWrite) 
                    continue;
                
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate) 
                    continue;
                
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType)) 
                    continue;

                // Passed all tests, lets set the value
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
            }
        }
    }
}