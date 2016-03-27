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

namespace GenUtils
{
    public delegate T TreeNodeMaker<T>(string text, T[] children);

    public interface TreeNodeProvider
    {
        T[] treeNodes<T>(TreeNodeMaker<T> maker);
    }

    public class RecursiveTreeNode
    {
        public readonly string text;
        public readonly RecursiveTreeNode[] children;

        public RecursiveTreeNode(string text, RecursiveTreeNode[] children)
        {
            this.text = text;
            this.children = children;
        }

        public static T[] recurse<T, U>(TreeNodeMaker<T> maker, U[] source) where U : TreeNodeProvider
        {
            T[] children = new T[source.Length];
            for (int i = 0; i < children.Length; i++)
                children[i] = maker(i.ToString(), source[i].treeNodes(maker));
            return children;
        }
    }
}
