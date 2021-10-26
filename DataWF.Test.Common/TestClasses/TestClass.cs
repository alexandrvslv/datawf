using System.Runtime.CompilerServices;

namespace DataWF.Test.Common
{
    public class TestClass
    {
        public int Field;
        public string Name { get; set; }
        public TestStruct Struct { get; set; }
        public TestClass Group { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int R { get; } = 1;
        public int[] Array { get; set; }

        public int this[int index]
        {
            get => Array[index];
            set => Array[index] = value;
        }
    }
}
