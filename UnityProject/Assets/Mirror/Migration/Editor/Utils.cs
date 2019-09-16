using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;
#pragma warning disable 618

namespace Mirror.MigrationUtilities {
    public class Utils : MonoBehaviour {
        public static readonly UTF8Encoding Utf8NoBomEncoding = new UTF8Encoding(false);
        public static readonly UTF8Encoding Utf8BomEncoding = new UTF8Encoding(true);
        public static readonly UTF7Encoding Utf7BomEncoding = new UTF7Encoding();
        public static readonly UnicodeEncoding Utf16LeBomEncoding = new UnicodeEncoding(false, true);
        public static readonly UnicodeEncoding Utf16BeBomEncoding = new UnicodeEncoding(true, true);
        public static readonly UTF32Encoding Utf32LeBomEncoding = new UTF32Encoding(false, true);
        public static readonly UTF32Encoding Utf32BeBomEncoding = new UTF32Encoding(true, true);
        public static readonly Encoding Latin1Encoding = Encoding.GetEncoding("ISO-8859-1");

        public static bool ReplaceNetworkComponent<TSource, TDestination>(GameObject prefab)
            where TSource : Component
            where TDestination : Component {
            
            TSource unetNetworkComponent = prefab.GetComponent<TSource>();
            if (unetNetworkComponent != null) {
                //ignore deriving classes (they should be changed by the script conversion)
                if (unetNetworkComponent.GetType() != typeof(TSource)) {
                    Debug.Log("Ignoring deriving component on: " + prefab.name);
                    return false;
                }

                // check for mirror component
                TDestination mirrorNetworkComponent = prefab.AddComponent<TDestination>();
                if (mirrorNetworkComponent == null) {
                    mirrorNetworkComponent = prefab.GetComponent<TDestination>();
                }

                // copy values
                CopyProperties(unetNetworkComponent, mirrorNetworkComponent);

                // destroy UNET component
                DestroyImmediate(unetNetworkComponent, true);
                return true;
            }

            return false;
        }

        public static bool ReplaceNetworkIdentity(GameObject prefab) {
            UnityEngine.Networking.NetworkIdentity unetNetworkComponent = prefab.GetComponent<UnityEngine.Networking.NetworkIdentity>();
            if (unetNetworkComponent != null) {
                Mirror.NetworkIdentity mirrorNetworkComponent = null;
                byte i = 0;
                while (prefab.GetComponent<Mirror.NetworkIdentity>() == null && i < 3) {
                    try {
                        mirrorNetworkComponent = prefab.AddComponent<Mirror.NetworkIdentity>();
                    }
                    catch {
                        // ignore random errors
                    }
                    i++;
                }
                if (mirrorNetworkComponent == null) {
                    mirrorNetworkComponent = prefab.GetComponent<Mirror.NetworkIdentity>();
                }

                if (mirrorNetworkComponent == null) {
                    Debug.LogError("Could not add NetworkIdentity to:" + prefab.name);
                    return false;
                }

                mirrorNetworkComponent.localPlayerAuthority = unetNetworkComponent.localPlayerAuthority;
                mirrorNetworkComponent.serverOnly = unetNetworkComponent.serverOnly;

                // destroy UNET component
                DestroyImmediate(unetNetworkComponent, true);
                return true;
            }

            return false;
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

        public static Encoding GetEncoding(string filename) {
            return GetEncoding(filename, Utf8NoBomEncoding);
        }

        public static Encoding GetEncoding(string filename, Encoding defaultEncoding) {
            // Read the BOM
            byte[] bom = new byte[4];
            using (FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                file.Read(bom, 0, 4);
            }

            // Analyze the BOM https://en.wikipedia.org/wiki/Byte_order_mark#Byte_order_marks_by_encoding
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return Utf7BomEncoding;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return Utf8BomEncoding;
            if (bom[0] == 0xff && bom[1] == 0xfe && bom[2] == 0 && bom[3] == 0) return Utf32LeBomEncoding;
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return Utf32BeBomEncoding;
            if (bom[0] == 0xff && bom[1] == 0xfe) return Utf16LeBomEncoding;
            if (bom[0] == 0xfe && bom[1] == 0xff) return Utf16BeBomEncoding;
            return defaultEncoding;
        }
    }
}
