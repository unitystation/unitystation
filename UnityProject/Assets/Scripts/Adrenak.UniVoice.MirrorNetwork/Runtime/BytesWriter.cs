using System;

using System.Text;

using UnityEngine;

using System.Collections.Generic;



namespace Adrenak.BRW {

    /// <summary>

    /// A utility to write data into a byte array

    /// </summary>

    public class BytesWriter {

        List<byte> m_Bytes;



        /// <summary>

        /// Creates a new <see cref="BytesWriter"/> instance

        /// </summary>

        public BytesWriter() {

            m_Bytes = new List<byte>();

        }



        /// <summary>

        /// Returns the bytes written to the instance so far

        /// </summary>

        public byte[] Bytes {

            get { return m_Bytes.ToArray(); }

        }



        // Default types

        /// <summary>

        /// Writes a short to the internal byte list

        /// </summary>

        /// <param name="value"></param>

        /// <returns></returns>

        public BytesWriter WriteShort(Int16 value) {

            var bytes = BitConverter.GetBytes(value);

            EndianUtility.EndianCorrection(bytes);

            WriteBytes(bytes);

            return this;

        }



        /// <summary>

        /// Writes an int to the internal byte list

        /// </summary>

        /// <param name="value"></param>

        /// <returns></returns>

        public BytesWriter WriteInt(Int32 value) {

            var bytes = BitConverter.GetBytes(value);

            EndianUtility.EndianCorrection(bytes);

            WriteBytes(bytes);

            return this;

        }



        /// <summary>

        /// Writes a long to the internal byte list

        /// </summary>

        /// <param name="value"></param>

        /// <returns></returns>

        public BytesWriter WriteLong(Int64 value) {

            var bytes = BitConverter.GetBytes(value);

            EndianUtility.EndianCorrection(bytes);

            WriteBytes(bytes);

            return this;

        }



        /// <summary>

        /// Writes a float to the internal byte list

        /// </summary>

        /// <param name="value"></param>

        /// <returns></returns>

        public BytesWriter WriteFloat(Single value) {

            var bytes = BitConverter.GetBytes(value);

            EndianUtility.EndianCorrection(bytes);

            WriteBytes(bytes);

            return this;

        }



        /// <summary>

        /// Writes a double to the internal byte list

        /// </summary>

        /// <param name="value"></param>

        /// <returns></returns>

        public BytesWriter WriteDouble(double value) {

            var bytes = BitConverter.GetBytes(value);

            EndianUtility.EndianCorrection(bytes);

            WriteBytes(bytes);

            return this;

        }



        /// <summary>

        /// Writes a char to the internal byte list

        /// </summary>

        /// <param name="val"></param>

        /// <returns></returns>

        public BytesWriter WriteChar(char val) {

            WriteBytes(BitConverter.GetBytes(val));

            return this;

        }



        /// <summary>

        /// Writes a short array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteShortArray(Int16[] array) {

            WriteInt(array.Length);



            foreach (var e in array)

                WriteShort(e);

            return this;

        }



        /// <summary>

        /// Writes an int array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteIntArray(Int32[] array) {

            WriteInt(array.Length);



            foreach (var e in array)

                WriteInt(e);

            return this;

        }



        /// <summary>

        /// Writes a long array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteLongArray(Int64[] array) {

            WriteInt(array.Length);



            foreach (var e in array)

                WriteLong(e);

            return this;

        }



        /// <summary>

        /// Writes a float array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteFloatArray(Single[] array) {

            WriteInt(array.Length);



            foreach (var e in array)

                WriteFloat(e);

            return this;

        }



        /// <summary>

        /// Writes a double array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteDoubleArray(Double[] array) {

            WriteInt(array.Length);



            foreach (var e in array)

                WriteDouble(e);

            return this;

        }



        /// <summary>

        /// Writes a string to the internal byte list

        /// </summary>

        /// <param name="str"></param>

        /// <returns></returns>

        public BytesWriter WriteString(string str) {

            var strB = Encoding.UTF8.GetBytes(str);

            WriteInt(strB.Length);

            WriteBytes(strB);

            return this;

        }



        // Unity types

        /// <summary>

        /// Writes a Vector3 to the internal byte list

        /// </summary>

        /// <param name="value"></param>

        /// <returns></returns>

        public BytesWriter WriteVector3(Vector3 value) {

            var xbytes = BitConverter.GetBytes(value.x);

            var ybytes = BitConverter.GetBytes(value.y);

            var zbytes = BitConverter.GetBytes(value.z);



            EndianUtility.EndianCorrection(xbytes);

            EndianUtility.EndianCorrection(ybytes);

            EndianUtility.EndianCorrection(zbytes);



            WriteBytes(xbytes);

            WriteBytes(ybytes);

            WriteBytes(zbytes);



            return this;

        }



        /// <summary>

        /// Writes a Vector3 array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteVector3Array(Vector3[] array) {

            var lenB = BitConverter.GetBytes(array.Length);

            EndianUtility.EndianCorrection(lenB);

            WriteBytes(lenB);



            foreach (var e in array)

                WriteVector3(e);



            return this;

        }



        /// <summary>

        /// Writes a Vector2 to the internal byte list

        /// </summary>

        /// <param name="value"></param>

        /// <returns></returns>

        public BytesWriter WriteVector2(Vector2 value) {

            var xbytes = BitConverter.GetBytes(value.x);

            var ybytes = BitConverter.GetBytes(value.y);



            EndianUtility.EndianCorrection(xbytes);

            EndianUtility.EndianCorrection(ybytes);



            WriteBytes(xbytes);

            WriteBytes(ybytes);



            return this;

        }



        /// <summary>

        /// Writes a Vector2 array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteVector2Array(Vector2[] array) {

            var lenB = BitConverter.GetBytes(array.Length);

            EndianUtility.EndianCorrection(lenB);

            WriteBytes(lenB);



            foreach (var e in array)

                WriteVector2(e);



            return this;

        }



        /// <summary>

        /// Writes a Rect to the internal byte list

        /// </summary>

        /// <param name="rect"></param>

        /// <returns></returns>

        public BytesWriter WriteRect(Rect rect) {

            var xbytes = BitConverter.GetBytes(rect.x);

            var ybytes = BitConverter.GetBytes(rect.y);

            var wbytes = BitConverter.GetBytes(rect.width);

            var hbytes = BitConverter.GetBytes(rect.height);



            EndianUtility.EndianCorrection(xbytes);

            EndianUtility.EndianCorrection(ybytes);

            EndianUtility.EndianCorrection(wbytes);

            EndianUtility.EndianCorrection(hbytes);



            WriteBytes(xbytes);

            WriteBytes(ybytes);

            WriteBytes(wbytes);

            WriteBytes(hbytes);



            return this;

        }



        /// <summary>

        /// Writes a Rect array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteRectArray(Rect[] array) {

            var lenB = BitConverter.GetBytes(array.Length);

            EndianUtility.EndianCorrection(lenB);

            WriteBytes(lenB);



            foreach (var e in array)

                WriteRect(e);



            return this;

        }



        /// <summary>

        /// Writes a Color32 to the internal byte list

        /// </summary>

        /// <param name="color"></param>

        /// <returns></returns>

        public BytesWriter WriteColor32(Color32 color) {

            WriteByte(color.r);

            WriteByte(color.g);

            WriteByte(color.b);

            WriteByte(color.a);

            return this;

        }



        /// <summary>

        /// Writes a Color32 array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteColor32Array(Color32[] array) {

            WriteInt(array.Length);



            for (int i = 0; i < array.Length; i++)

                WriteColor32(array[i]);

            return this;

        }



        /// <summary>

        /// Writes a Color to the internal byte list

        /// </summary>

        /// <param name="color"></param>

        /// <returns></returns>

        public BytesWriter WriteColor(Color color) {

            WriteFloat(color.r);

            WriteFloat(color.g);

            WriteFloat(color.b);

            WriteFloat(color.a);

            return this;

        }



        /// <summary>

        /// Writes a Color array to the internal byte list

        /// </summary>

        /// <param name="array"></param>

        /// <returns></returns>

        public BytesWriter WriteColorArray(Color[] array) {

            WriteInt(array.Length);



            for (int i = 0; i < array.Length; i++)

                WriteColor(array[i]);

            return this;

        }



        // CORE

        /// <summary>

        /// Writes a byte array to the internal byte list along with the

        /// length of the array

        /// </summary>

        /// <param name="bytes"></param>

        public BytesWriter WriteByteArray(byte[] bytes) {

            WriteInt(bytes.Length);

            WriteBytes(bytes);

            return this;

        }



        /// <summary>

        /// Writes a byte array to the internal byte list

        /// </summary>

        /// <param name="block"></param>

        public BytesWriter WriteBytes(byte[] block) {

            foreach (var b in block)

                m_Bytes.Add(b);

            return this;

        }



        /// <summary>

        /// Writes a byte to the internal byte list

        /// </summary>

        /// <param name="b"></param>

        public BytesWriter WriteByte(byte b) {

            m_Bytes.Add(b);

            return this;

        }



        /// <summary>

        /// Over writes a byte to the internal byte list at the given index

        /// </summary>

        /// <param name="index"></param>

        /// <param name="b"></param>

        public BytesWriter OverwriteByte(int index, byte b) {

            try {

                m_Bytes[index] = b;

                return this;

            }

            catch {

                throw;

            }

        }



        /// <summary>

        /// Over writes the internal byte list with an array at the given index

        /// </summary>

        /// <param name="index"></param>

        /// <param name="bytes"></param>

        public BytesWriter OverwriteBytes(int index, byte[] bytes) {

            try {

                for (int i = index; i < index + bytes.Length; i++)

                    OverwriteByte(i, bytes[i - index]);

                return this;

            }

            catch {

                throw;

            }

        }

    }

}