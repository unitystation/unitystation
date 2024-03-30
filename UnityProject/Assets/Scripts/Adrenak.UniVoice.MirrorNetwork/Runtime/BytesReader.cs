using System;
using UnityEngine;
using System.Text;



namespace Adrenak.BRW {

    /// <summary>

    /// A utility to read objects from a byte array

    /// </summary>

    public class BytesReader {

        public int Cursor { get; private set; }

        byte[] m_Array;



        /// <summary>

        /// Creates an instance with the given array

        /// </summary>

        /// <param name="array"></param>

        public BytesReader(byte[] array) {

            m_Array = array;

            Cursor = 0;

        }



        // Default types

        /// <summary>

        /// Attempts to return a short at the current cursor position

        /// </summary>

        public Int16 ReadShort() {

            var bytes = ReadBytes(2);

            EndianUtility.EndianCorrection(bytes);

            return BitConverter.ToInt16(bytes, 0);

        }



        /// <summary>

        /// Attempts to return a short array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Int16[] ReadShortArray() {

            var len = ReadInt();

            var result = new Int16[len];



            for (int i = 0; i < result.Length; i++)

                result[i] = ReadShort();

            return result;

        }



        /// <summary>

        /// Attempts to return an int at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Int32 ReadInt() {

            var bytes = ReadBytes(4);

            EndianUtility.EndianCorrection(bytes);

            return BitConverter.ToInt32(bytes, 0);

        }



        /// <summary>

        /// Attempts to return an int array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Int32[] ReadIntArray() {

            var len = ReadInt();

            var result = new Int32[len];



            for (int i = 0; i < result.Length; i++)

                result[i] = ReadInt();

            return result;

        }



        /// <summary>

        /// Attempts to return a long at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Int64 ReadLong() {

            var bytes = ReadBytes(8);

            EndianUtility.EndianCorrection(bytes);

            return BitConverter.ToInt64(bytes, 0);

        }



        /// <summary>

        /// Attempts to return a long array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Int64[] ReadLongArray() {

            var len = ReadLong();

            var result = new Int64[len];



            for (int i = 0; i < result.Length; i++)

                result[i] = ReadLong();

            return result;

        }



        /// <summary>

        /// Attempts to return a float at the current cursor position

        /// </summary>

        /// <returns></returns>

        public float ReadFloat() {

            var bytes = ReadBytes(4);

            EndianUtility.EndianCorrection(bytes);

            return BitConverter.ToSingle(bytes, 0);

        }



        /// <summary>

        /// Attempts to return a float array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Single[] ReadFloatArray() {

            var len = ReadInt();

            var result = new Single[len];



            for (int i = 0; i < result.Length; i++)

                result[i] = ReadFloat();

            return result;

        }



        /// <summary>

        /// Attempts to return a double at the current cursor position

        /// </summary>

        /// <returns></returns>

        public double ReadDouble() {

            var bytes = ReadBytes(8);

            EndianUtility.EndianCorrection(bytes);

            return BitConverter.ToDouble(bytes, 0);

        }



        /// <summary>

        /// Attempts to return a double array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Double[] ReadDoubleArray() {

            var len = ReadInt();

            var result = new Double[len];



            for (int i = 0; i < result.Length; i++)

                result[i] = ReadShort();

            return result;

        }



        /// <summary>

        /// Attempts to return a char at the current cursor position

        /// </summary>

        /// <returns></returns>

        public char ReadChar() {

            return BitConverter.ToChar(ReadBytes(2), 0);

        }



        /// <summary>

        /// Attempts to return a string at the current cursor position

        /// </summary>

        /// <returns></returns>

        public string ReadString() {

            var len = ReadInt();

            return Encoding.UTF8.GetString(ReadBytes(len));

        }



        // Unity types

        /// <summary>

        /// Attempts to return a Vector2 at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Vector2 ReadVector2() {

            return new Vector2(ReadFloat(), ReadFloat());

        }



        /// <summary>

        /// Attempts to return a Vector2 array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Vector2[] ReadVector2Array() {

            var len = ReadInt();

            var result = new Vector2[len];



            for (int i = 0; i < len; i++)

                result[i] = ReadVector2();

            return result;

        }



        /// <summary>

        /// Attempts to return a Vector3 at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Vector3 ReadVector3() {

            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());

        }



        /// <summary>

        /// Attempts to return a Vector3 array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Vector3[] ReadVector3Array() {

            var len = ReadInt();

            var result = new Vector3[len];



            for (int i = 0; i < len; i++)

                result[i] = ReadVector3();

            return result;

        }



        /// <summary>

        /// Attempts to return a Rect at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Rect ReadRect() {

            return new Rect(

                ReadFloat(),

                ReadFloat(),

                ReadFloat(),

                ReadFloat()

            );

        }



        /// <summary>

        /// Attempts to return a Rect array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Rect[] ReadRectArray() {

            var len = ReadInt();

            var result = new Rect[len];



            for (int i = 0; i < len; i++)

                result[i] = ReadRect();

            return result;

        }



        /// <summary>

        /// Attempts to return a Color32 at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Color32 ReadColor32() {

            byte r, g, b, a;

            ReadByte(out r);

            ReadByte(out g);

            ReadByte(out b);

            ReadByte(out a);

            return new Color32(r, g, b, a);

        }



        /// <summary>

        /// Attempts to return a Color32 array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Color32[] ReadColor32Array() {

            int len = ReadInt();

            var result = new Color32[len];



            for (int i = 0; i < result.Length; i++)

                result[i] = ReadColor32();

            return result;

        }



        /// <summary>

        /// Attempts to return a Color at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Color ReadColor() {

            return new Color(

                ReadFloat(),

                ReadFloat(),

                ReadFloat(),

                ReadFloat()

            );

        }



        /// <summary>

        /// Attempts to return a Color array at the current cursor position

        /// </summary>

        /// <returns></returns>

        public Color[] ReadColorArray() {

            int len = ReadInt();

            var result = new Color[len];



            for (int i = 0; i < result.Length; i++)

                result[i] = ReadColor();

            return result;

        }



        // CORE

        /// <summary>

        /// Attempts to return a byte array of a detected length at the current cursor position

        /// </summary>

        /// <returns></returns>

        public byte[] ReadByteArray() {

            try {

                int length = ReadInt();

                return ReadBytes(length);

            }

            catch {

                return null;

            }

        }



        /// <summary>

        /// Attempts to return a byte at the current cursor position

        /// </summary>

        /// <param name="result"></param>

        /// <returns></returns>

        public bool ReadByte(out byte result) {

            try {

                result = m_Array[Cursor];

                Cursor++;

                return true;

            }

            catch {

                result = 0;

                return false;

            }

        }





        /// <summary>

        /// Attempts to return a byte array of a given length at the current cursor position

        /// </summary>

        /// <param name="length"></param>

        /// <returns></returns>

        public byte[] ReadBytes(int length) {

            try {

                var result = ReadBytesDiscrete(Cursor, length);

                Cursor += length;

                return result;

            }

            catch {

                return null;

            }

        }



        /// <summary>

        /// Attempts to return the byte array of the given length from a given index

        /// from the internal byte array

        /// </summary>

        /// <param name="index">The index in the internal byte array from where to read the byets</param>

        /// <param name="length">The number of bytes to read from the internal byte array</param>

        /// <returns></returns>

        public byte[] ReadBytesDiscrete(int index, int length) {

            try {

                byte[] b = new byte[length];

                Buffer.BlockCopy(m_Array, index, b, 0, length);

                return b;

            }

            catch {

                return null;

            }

        }

    }

}