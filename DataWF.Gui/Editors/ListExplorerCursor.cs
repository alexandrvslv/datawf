using System.Collections.Generic;

namespace DataWF.Gui
{
    public class ListExplorerCursor
    {
        private LinkedListNode<ListExplorerNode> current;

        public ListExplorerNode Current
        {
            get { return current?.Value; }
            set
            {
                if (Current != value)
                {
                    var temp = Queue.Find(value);
                    if (temp == null)
                    {
                        if (current == null)
                            current = Queue.AddLast(value);
                        else
                            current = Queue.AddAfter(current, value);
                    }
                    else
                        current = temp;
                }
            }
        }

        public LinkedList<ListExplorerNode> Queue { get; internal set; } = new LinkedList<ListExplorerNode>();

        public ListExplorerNode Prev()
        {
            var temp = current?.Previous;
            if (temp != null)
                current = temp;
            return current.Value;
        }

        public ListExplorerNode Next()
        {
            var temp = current?.Next;
            if (temp != null)
                current = temp;
            return current.Value;
        }

    }

}
