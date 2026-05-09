using System;
using Unity.Collections;
using UnityEngine;

namespace Helper
{
    public static class ArrayHelper
    {
        public static Vector3 GetAveragePosition(Vector3[] positions)
        {
            Vector3 avgPos = Vector3.zero;

            foreach (Vector3 position in positions)
            {
                avgPos += position;
            }
            
            return avgPos / positions.Length;
        }
        
        public static void Set2D<T>(this NativeArray<T> array, int x, int y, T value, int width) where T : unmanaged
        {
            int flatIndex = FlattenIndex(x, y, width);
            
            if (flatIndex < 0 || flatIndex >= array.Length)
            {
                throw new IndexOutOfRangeException($"Index {flatIndex} is out of range {array.Length}. [{x},{y}], width:{width}");
            }
            
            array[flatIndex] = value;
        }
 
        
        public static T Get2D<T>(this NativeArray<T> array, int x, int y, int width) where T : unmanaged
        {
            int flatIndex = FlattenIndex(x, y, width);

            if (flatIndex < 0 || flatIndex >= array.Length)
            {
                throw new IndexOutOfRangeException($"Index {flatIndex} is out of range {array.Length}. ({x},{y}), width:{width}");
            }
            
            return array[flatIndex];
        }
 
        public static int FlattenIndex(int x, int y, int width) => y * width + x;
    }
}