namespace DataWF.Collections.Pulls;

public static class PullExtensions
{
        public static T GetValue<T>(this Pull pull, in PullHandler handler)
          => ((GenericPull<T>)pull).GetValue(handler);

        public static void SetValue<T>(this Pull pull, in PullHandler handler, T value)
          => ((GenericPull<T>)pull).SetValue(handler, value);

        public static T GetValue<T>(this Pull pull, int index)
          => ((GenericPull<T>)pull).GetValue(index);

        public static void SetValue<T>(this Pull pull, int index, T value)
          => ((GenericPull<T>)pull).SetValue(index, value);

        public static T GetValue<T>(this Pull pull, int block, int blockIndex)
          => ((GenericPull<T>)pull).GetValue(block, blockIndex);

        public static void SetValue<T>(this Pull pull, int block, int blockIndex, T value)
          => ((GenericPull<T>)pull).SetValue(block, blockIndex, value);

        public static T GetValue<T>(this Pull pull, IPullHandler target)
          => ((GenericPull<T>)pull).GetValue(target);

        public static void SetValue<T>(this Pull pull, IPullHandler target, T value)
          => ((GenericPull<T>)pull).SetValue(target, value);
}
