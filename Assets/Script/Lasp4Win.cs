using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Lasp4Win
{
    class LaspWinProgress
    {
        public bool ExecuteProgram(string exeFilename = "run_lasp.bat", string workDir= ".\\", string args ="")
        {
            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo();
            info.FileName = exeFilename;
            info.WorkingDirectory = workDir;
            info.UseShellExecute = false;
            //info.CreateNoWindow = true;
            info.Arguments = args;

            System.Diagnostics.Process task = null;
            bool rt = true;
            try
            {
                Debug.Log("ExecuteProgram:" + args);

                task = System.Diagnostics.Process.Start(info);
                if (task != null)
                {
                    task.WaitForExit(100000);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error:" + e.ToString());
                return false;
            }
            finally
            {
                if (task != null && task.HasExited)
                {
                    rt = (task.ExitCode == 0);
                }
            }

            return rt;
        }
    }
   
}
