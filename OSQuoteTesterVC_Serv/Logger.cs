using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;


namespace OSQuoteTester
{
    public class Logger
    {
        private const string FILE_NAME = "OSQuoteLog.txt";
         
        public void Write( string strMsg )
        {
            using (StreamWriter w = File.AppendText(FILE_NAME))
            {
                Log(strMsg, w);
                // Close the writer and underlying file.
                w.Close();
            } 
        }

        static void Log(String logMessage, TextWriter w)
        {

            string strTime = DateTime.Now.ToString("hh:mm:ss");

            w.Write("\r\nLog Entry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongDateString(), strTime);
            w.WriteLine("  :{0}", logMessage);
            w.WriteLine("-------------------------------");
            // Update the underlying file.
            w.Flush();
        }

    }

}
