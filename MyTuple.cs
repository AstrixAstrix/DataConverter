using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
//using DevExpress.Persistent.BaseImpl;

namespace DataConverter
{

    public class MyTuple<T1, T2> 
    {



        private T1 mItem1;
        private T2 mItem2;

        public T1 Item1
        {
            get { return mItem1; }
            set
            {
                mItem1 = value;
            }
        }
        public T2 Item2
        {
            get { return mItem2; }
            set
            {
                mItem2 = value;
            }
        }
        public MyTuple()
        {

        }
        public MyTuple(T1 item1, T2 item2)
        {
            mItem1 = item1;
            mItem2 = item2;
        }





        public override  string ToString()
        {

            return $"1: {Item1}\t2: {Item2}";
        }



        int Length => 2;

        /// <summary>
        /// Get the element at position <param name="index"/>.
        /// </summary>

    }
}
