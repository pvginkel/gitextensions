using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitCommands
{

    /// <summary>
    /// Base class for structured git command
    /// </summary>
    public abstract class GitCommand
    {

        public abstract void CollectArguments(List<string> argumentsList);

        public abstract string GitComandName();

        public abstract bool AccessesRemote();

        public virtual string ToLine()
        {
            List<string> argumentsList = new List<string>();
            CollectArguments(argumentsList);
            String args = null;
            foreach (string s in argumentsList)
                args = args.Join(" ", s);
 
            return GitComandName().Join(" ", args);            
        }

        public override string ToString()
        {
            return ToLine();
        }

    }
}
