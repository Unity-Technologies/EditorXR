namespace Unity.Labs.EditorXR
{
    class Tuple<T1, T2>
    {
        public T1 firstElement;
        public T2 secondElement;

        internal Tuple(T1 firstElement, T2 secondElement)
        {
            this.firstElement = firstElement;
            this.secondElement = secondElement;
        }
    }

    class Tuple<T1, T2, T3>
    {
        public T1 firstElement;
        public T2 secondElement;
        public T3 thirdElement;

        internal Tuple(T1 firstElement, T2 secondElement, T3 thirdElement)
        {
            this.firstElement = firstElement;
            this.secondElement = secondElement;
            this.thirdElement = thirdElement;
        }
    }

    class Tuple<T1, T2, T3, T4>
    {
        public T1 firstElement;
        public T2 secondElement;
        public T3 thirdElement;
        public T4 fourthElement;

        internal Tuple(T1 firstElement, T2 secondElement, T3 thirdElement, T4 fourthElement)
        {
            this.firstElement = firstElement;
            this.secondElement = secondElement;
            this.thirdElement = thirdElement;
            this.fourthElement = fourthElement;
        }
    }

    class Tuple<T1, T2, T3, T4, T5>
    {
        public T1 firstElement;
        public T2 secondElement;
        public T3 thirdElement;
        public T4 fourthElement;
        public T5 fifthElement;

        internal Tuple(T1 firstElement, T2 secondElement, T3 thirdElement, T4 fourthElement, T5 fifthElement)
        {
            this.firstElement = firstElement;
            this.secondElement = secondElement;
            this.thirdElement = thirdElement;
            this.fourthElement = fourthElement;
            this.fifthElement = fifthElement;
        }
    }
}
