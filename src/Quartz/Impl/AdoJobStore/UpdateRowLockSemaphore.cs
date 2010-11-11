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
using System.Data;
using System.Globalization;
using System.Threading;

namespace Quartz.Impl.AdoJobStore
{
    /// <summary>
    /// Provide thread/resource locking in order to protect
    /// resources from being altered by multiple threads at the same time using
    /// a db row update.
    /// </summary>
    /// <remarks>
    /// <b>Note:</b> This Semaphore implementation is useful for databases that do
    /// not support row locking via "SELECT FOR UPDATE" type syntax, for example
    /// Microsoft SQLServer (MSSQL).
    /// </remarks>
    /// <author>Marko Lahma (.NET)</author>
    public class UpdateLockRowSemaphore : DBSemaphore
    {
        public static readonly string SqlUpdateForLock =
            string.Format(CultureInfo.InvariantCulture, "UPDATE {0}{1} SET {2} = {3} WHERE {4} = @lockName", TablePrefixSubst, TableLocks, ColumnLockName,
                          ColumnLockName, ColumnLockName);

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateLockRowSemaphore"/> class.
        /// </summary>
        public UpdateLockRowSemaphore(IDbProvider provider)
            : base(DefaultTablePrefix, null, SqlUpdateForLock, provider)
        {
        }
        
        /// <summary>
        /// Execute the SQL that will lock the proper database row.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="lockName"></param>
        /// <param name="expandedSQL"></param>
        protected override void ExecuteSQL(ConnectionAndTransactionHolder conn, string lockName, string expandedSQL)
        {
            try
            {
                using (IDbCommand cmd = AdoUtil.PrepareCommand(conn, expandedSQL))
                {
                    AdoUtil.AddCommandParameter(cmd, "lockName", lockName);

                    if (Log.IsDebugEnabled)
                    {
                        Log.Debug("Lock '" + lockName + "' is being obtained: " + Thread.CurrentThread.Name);
                    }

                    int numUpdate = cmd.ExecuteNonQuery();

                    if (numUpdate < 1)
                    {
                        throw new Exception(
                            AdoJobStoreUtil.ReplaceTablePrefix(
                                "No row exists in table " + TablePrefixSubst + TableLocks + " for lock named: " +
                                lockName, TablePrefix));
                    }
                }
            }
            catch (Exception sqle)
            {
                if (Log.IsDebugEnabled)
                {
                    Log.Debug(
                        "Lock '" + lockName + "' was not obtained by: " +
                        Thread.CurrentThread.Name);
                }

                throw new LockException(
                    "Failure obtaining db row lock: " + sqle.Message, sqle);
            }
        }

        protected string UpdateLockRowSQL
        {
            get { return SQL; }
            set { SQL = value; }
        }
    }
}