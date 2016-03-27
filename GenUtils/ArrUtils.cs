/**************************************************************************
 * Copyright 2016 Leon Poon
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 **************************************************************************/

using System;

namespace GenUtils
{
    public class ArrUtils
    {
        public static T[] join<T>(params T[][] arrs)
        {
            int len = 0;
            foreach (var a in arrs)
                len += a.Length;
            T[] arr = new T[len];
            len = 0;
            foreach (var a in arrs)
            {
                Array.Copy(a, 0, arr, len, a.Length);
                len += a.Length;
            }
            return arr;
        }
    }
}
