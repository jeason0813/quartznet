#region License
/* 
 * All content copyright Terracotta, Inc., unless otherwise indicated. All rights reserved. 
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not 
 * use this file except in compliance with the License. You may obtain a copy 
 * of the License at 
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0 
 *   
 * Unless required by applicable law or agreed to in writing, software 
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations 
 * under the License.
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Quartz.Listener
{
    /// <summary>
    /// Holds a List of references to JobListener instances and broadcasts all
    /// events to them (in order) - if the event is not excluded via filtering
    /// (read on).
    ///</summary>
    /// <remarks>
    /// <p>
    /// The broadcasting behavior of this listener to delegate listeners may be
    /// more convenient than registering all of the listeners directly with the
    /// Job, and provides the flexibility of easily changing which listeners
    /// get notified.
    /// </p>
    ///
    /// <p>
    /// You may also register a number of Regular Expression patterns to match
    /// the events against. If one or more patterns are registered, the broadcast
    /// will only take place if the event applies to a job who's name/group
    /// matches one or more of the patterns.
    /// </p>
    ///</remarks>
    /// <seealso cref="AddListener(IJobListener)" />
    /// <seealso cref="RemoveListener(IJobListener)" />
    /// <seealso cref="RemoveListener(string)" />
    /// <seealso cref="AddJobNamePattern(string)" />
    /// <seealso cref="AddJobGroupPattern(string)" />
    /// <author>James House</author>
    /// <author>Marko Lahma (.NET)</author>
    public class FilterAndBroadcastJobListener : IJobListener
    {
        private readonly string name;
        private readonly List<IJobListener> listeners;
        private readonly List<string> namePatterns = new List<string>();
        private readonly List<string> groupPatterns = new List<string>();

        /// <summary>
        /// Construct an instance with the given name.
        /// (Remember to add some delegate listeners!)
        /// </summary>
        /// <param name="name">the name of this instance</param>
        public FilterAndBroadcastJobListener(string name)
        {
            if (name == null)
            {
                throw new ArgumentException("Listener name cannot be null!");
            }
            this.name = name;
            listeners = new List<IJobListener>();
        }
        
        /// <summary>
        /// Construct an instance with the given name, and List of listeners.
        /// </summary>
        /// <param name="name">the name of this instance</param>
        /// <param name="listeners">the initial List of JobListeners to broadcast to</param>
        public FilterAndBroadcastJobListener(string name, IEnumerable<IJobListener> listeners) : this(name)
        {
            this.listeners.AddRange(listeners);
        }

        /// <summary>
        /// Get the name of the <see cref="IJobListener" />.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        public void AddListener(IJobListener listener)
        {
            listeners.Add(listener);
        }

        public bool RemoveListener(IJobListener listener)
        {
            if (listeners.Contains(listener))
            {
                listeners.Remove(listener);
                return true;
            }
            
            return false;
        }

        public bool RemoveListener(string listenerName)
        {
            for (int i = 0; i < listeners.Count; ++i)
            {
                IJobListener jl = listeners[i];
                if (jl.Name.Equals(listenerName))
                {
                    listeners.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        public IList<IJobListener> GetListeners()
        {
            return listeners.AsReadOnly();
        }


        /// <summary>
        /// If one or more name patterns are specified, only events relating to
        /// jobs who's name matches the given regular expression pattern
        /// will be dispatched to the delegate listeners.
        /// </summary>
        /// <param name="regularExpression"></param>
        public void AddJobNamePattern(string regularExpression)
        {
            if (regularExpression == null)
            {
                throw new ArgumentException("Expression cannot be null!");
            }

            namePatterns.Add(regularExpression);
        }

        public IList<string> JobNamePatterns
        {
            get { return namePatterns; }
        }

        /// <summary>
        /// If one or more group patterns are specified, only events relating to
        /// jobs who's group matches the given regular expression pattern
        /// will be dispatched to the delegate listeners.
        /// </summary>
        /// <param name="regularExpression"></param>
        public void AddJobGroupPattern(string regularExpression)
        {
            if (regularExpression == null)
            {
                throw new ArgumentException("Expression cannot be null!");
            }

            groupPatterns.Add(regularExpression);
        }

        public IList<string> JobGroupPatterns
        {
            get { return namePatterns; }
        }

        protected virtual bool ShouldDispatch(JobExecutionContext context)
        {
            JobDetail job = context.JobDetail;

            if (namePatterns.Count == 0 && groupPatterns.Count == 0)
            {
                return true;
            }

            foreach (string pat in groupPatterns)
            {
                Regex rex = new Regex(pat);
                if (rex.IsMatch(job.Group))
                {
                    return true;
                }
            }


            foreach (string pat in namePatterns)
            {
                Regex rex = new Regex(pat);
                if (rex.IsMatch(job.Name))
                {
                    return true;
                }
            }

            return false;
        }

        public void JobToBeExecuted(JobExecutionContext context)
        {
            if (!ShouldDispatch(context))
            {
                return;
            }

            foreach (IJobListener jl in listeners)
            {
                jl.JobToBeExecuted(context);
            }
        }

        public void JobExecutionVetoed(JobExecutionContext context)
        {
            if (!ShouldDispatch(context))
            {
                return;
            }

            foreach (IJobListener jl in listeners)
            {
                jl.JobExecutionVetoed(context);
            }
        }

        public void JobWasExecuted(JobExecutionContext context, JobExecutionException jobException)
        {
            if (!ShouldDispatch(context))
            {
                return;
            }

            foreach (IJobListener jl in listeners)
            {
                jl.JobWasExecuted(context, jobException);
            }
        }
    }
}